// Cascading Building → Room dropdown, used by the issue form and the asset form.
// Expects <select id="BuildingId" data-rooms-url="..."> and <select id="RoomId">.
// When the building changes, its rooms are loaded as JSON from data-rooms-url.
(function () {
    const building = document.getElementById("BuildingId");
    const room = document.getElementById("RoomId");
    if (!building || !room || !building.dataset.roomsUrl) {
        return;
    }

    function setRooms(rooms, placeholder) {
        room.innerHTML = "";
        room.add(new Option(placeholder, ""));
        rooms.forEach(r => room.add(new Option(r.text, r.id)));
        room.disabled = !building.value;
    }

    building.addEventListener("change", async function () {
        if (!building.value) {
            setRooms([], "-- Select a building first --");
            return;
        }
        setRooms([], "Loading rooms…");
        room.disabled = true;
        try {
            const url = building.dataset.roomsUrl + "?buildingId=" + encodeURIComponent(building.value);
            const response = await fetch(url, { headers: { "Accept": "application/json" } });
            const rooms = await response.json();
            setRooms(rooms, rooms.length ? "-- Select room --" : "No rooms in this building");
        } catch {
            setRooms([], "Could not load rooms. Please try again.");
        }
    });
})();
