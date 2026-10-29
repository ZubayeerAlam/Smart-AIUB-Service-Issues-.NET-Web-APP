using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Enums;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Assets;

namespace Smart_AIUB_Service_Issue.Tests.Services;

/// <summary>Asset registry, issue → asset link (same-room rule) and maintenance records.</summary>
public class AssetTests
{
    // ---------- Asset registry ----------

    [Fact]
    public async Task Save_creates_asset_with_uppercase_tag()
    {
        await using var d = await IssueTestData.CreateAsync();

        var id = await d.CreateAssetAsync(" prj-d301 ");

        var asset = await d.Services.Get<ApplicationDbContext>().Assets.AsNoTracking().SingleAsync(a => a.Id == id);
        Assert.Equal("PRJ-D301", asset.AssetTag);
        Assert.Equal(AssetStatus.Active, asset.Status);
    }

    [Fact]
    public async Task Save_rejects_duplicate_tag_even_with_different_case()
    {
        await using var d = await IssueTestData.CreateAsync();
        await d.CreateAssetAsync("PRJ-D301");

        var result = await d.Services.Get<IAssetService>().SaveAsync(d.AssetForm("prj-d301", d.C101));

        Assert.False(result.Succeeded);
        Assert.Equal(nameof(AssetFormViewModel.AssetTag), result.Field);
    }

    [Fact]
    public async Task Editing_an_asset_keeps_its_own_tag()
    {
        await using var d = await IssueTestData.CreateAsync();
        var id = await d.CreateAssetAsync("PRJ-D301");
        var form = d.AssetForm("PRJ-D301");
        form.Id = id;
        form.Name = "Renamed projector";

        var result = await d.Services.Get<IAssetService>().SaveAsync(form);

        Assert.True(result.Succeeded, result.Error);
    }

    [Fact]
    public async Task Save_rejects_room_that_is_not_in_the_selected_building()
    {
        await using var d = await IssueTestData.CreateAsync();
        var form = d.AssetForm("PRJ-X");
        form.BuildingId = d.C.Id; // D301 is in D-Building

        var result = await d.Services.Get<IAssetService>().SaveAsync(form);

        Assert.Equal(nameof(AssetFormViewModel.RoomId), result.Field);
    }

    [Fact]
    public async Task Save_rejects_inactive_asset_type()
    {
        await using var d = await IssueTestData.CreateAsync();
        d.ProjectorType.IsActive = false;
        await d.Services.Get<ApplicationDbContext>().SaveChangesAsync();

        var result = await d.Services.Get<IAssetService>().SaveAsync(d.AssetForm("PRJ-X"));

        Assert.Equal(nameof(AssetFormViewModel.AssetTypeId), result.Field);
    }

    [Fact]
    public void Warranty_cannot_end_before_purchase()
    {
        var form = new AssetFormViewModel
        {
            AssetTag = "PRJ-X", Name = "Projector", AssetTypeId = 1, BuildingId = 1, RoomId = 1,
            PurchaseDate = new DateOnly(2026, 5, 1),
            WarrantyExpiry = new DateOnly(2026, 1, 1)
        };
        var errors = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(form, new ValidationContext(form), errors, validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(AssetFormViewModel.WarrantyExpiry)));
    }

    // ---------- Issue → asset link ----------

    private static async Task<int> AssignedToItAsync(IssueTestData d)
    {
        var id = await d.ReportAsync();
        var result = await d.Services.Get<IIssueWorkflowService>()
            .AssignAsync(id, IssuePriority.High, d.ItTeam.Id, null, null, d.ActorFor(d.Admin, Roles.Admin));
        Assert.True(result.Succeeded, result.Error);
        return id;
    }

    [Fact]
    public async Task Team_staff_can_link_an_asset_in_the_same_room()
    {
        await using var d = await IssueTestData.CreateAsync();
        var issueId = await AssignedToItAsync(d);
        var assetId = await d.CreateAssetAsync("PRJ-D301", d.D301);

        var result = await d.Services.Get<IIssueService>()
            .LinkAssetAsync(issueId, assetId, d.ActorFor(d.ItStaff, Roles.SupportStaff));

        Assert.True(result.Succeeded, result.Error);
        var db = d.Services.Get<ApplicationDbContext>();
        Assert.Equal(assetId, (await db.Issues.AsNoTracking().SingleAsync(i => i.Id == issueId)).AssetId);
        Assert.Contains(db.IssueStatusHistory, h => h.IssueId == issueId && h.Note == "Linked to asset PRJ-D301");
    }

    [Fact]
    public async Task Asset_in_another_room_cannot_be_linked()
    {
        await using var d = await IssueTestData.CreateAsync();
        var issueId = await AssignedToItAsync(d);            // issue is in D301
        var otherRoomAsset = await d.CreateAssetAsync("PRJ-C101", d.C101);

        var result = await d.Services.Get<IIssueService>()
            .LinkAssetAsync(issueId, otherRoomAsset, d.ActorFor(d.Admin, Roles.Admin));

        Assert.False(result.Succeeded);
        Assert.Null((await d.Services.Get<ApplicationDbContext>().Issues.AsNoTracking().SingleAsync()).AssetId);
    }

    [Fact]
    public async Task Reporter_and_other_teams_cannot_link_assets()
    {
        await using var d = await IssueTestData.CreateAsync();
        var issueId = await AssignedToItAsync(d);
        var assetId = await d.CreateAssetAsync("PRJ-D301", d.D301);
        var issues = d.Services.Get<IIssueService>();

        Assert.False((await issues.LinkAssetAsync(issueId, assetId, d.ActorFor(d.Reporter, Roles.Student))).Succeeded);
        Assert.False((await issues.LinkAssetAsync(issueId, assetId, d.ActorFor(d.ElectricalStaff, Roles.SupportStaff))).Succeeded);
    }

    // ---------- Maintenance records ----------

    private static MaintenanceFormViewModel MaintenanceForm(int assetId, int? issueId = null) => new()
    {
        AssetId = assetId,
        IssueId = issueId,
        Type = MaintenanceType.Corrective,
        Description = "Replaced the projector lamp.",
        PerformedAtLocal = DhakaTime.NowInDhaka().AddHours(-1),
        Cost = 4500m,
        AssetStatus = AssetStatus.Active
    };

    [Fact]
    public async Task Maintenance_record_is_saved_and_updates_asset_status()
    {
        await using var d = await IssueTestData.CreateAsync();
        var issueId = await AssignedToItAsync(d);
        var assetId = await d.CreateAssetAsync("PRJ-D301", d.D301);
        var it = d.ActorFor(d.ItStaff, Roles.SupportStaff);
        Assert.True((await d.Services.Get<IIssueService>().LinkAssetAsync(issueId, assetId, it)).Succeeded);
        var form = MaintenanceForm(assetId, issueId);
        form.AssetStatus = AssetStatus.UnderMaintenance;

        var result = await d.Services.Get<IMaintenanceService>().CreateAsync(form, it);

        Assert.True(result.Succeeded, result.Error);
        var details = await d.Services.Get<IAssetService>().GetDetailsAsync(assetId);
        var record = Assert.Single(details!.Maintenance);
        Assert.Equal(issueId, record.IssueId);
        Assert.Equal(4500m, details.TotalCost);
        Assert.Equal(AssetStatus.UnderMaintenance, details.Status);
        Assert.Single(details.Issues);
    }

    [Fact]
    public async Task Maintenance_cannot_reference_an_issue_that_is_not_linked_to_the_asset()
    {
        await using var d = await IssueTestData.CreateAsync();
        var issueId = await AssignedToItAsync(d);
        var assetId = await d.CreateAssetAsync("PRJ-D301", d.D301);

        var result = await d.Services.Get<IMaintenanceService>()
            .CreateAsync(MaintenanceForm(assetId, issueId), d.ActorFor(d.ItStaff, Roles.SupportStaff));

        Assert.Equal(nameof(MaintenanceFormViewModel.IssueId), result.Field);
    }

    [Fact]
    public async Task Maintenance_date_cannot_be_in_the_future()
    {
        await using var d = await IssueTestData.CreateAsync();
        var assetId = await d.CreateAssetAsync("PRJ-D301");
        var form = MaintenanceForm(assetId);
        form.PerformedAtLocal = DhakaTime.NowInDhaka().AddDays(2);

        var result = await d.Services.Get<IMaintenanceService>().CreateAsync(form, d.ActorFor(d.ItStaff, Roles.SupportStaff));

        Assert.Equal(nameof(MaintenanceFormViewModel.PerformedAtLocal), result.Field);
    }
}
