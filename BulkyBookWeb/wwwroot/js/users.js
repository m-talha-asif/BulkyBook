var userDataTable;

$(document).ready(function () {
    loadDataTable();
})

function loadDataTable() {
    userDataTable = $('#tblData').DataTable({
        ajax: '/admin/user/getall',
        columns: [
            { data: 'name', "width": "15%" },
            { data: 'email', "width": "25%" },
            { data: 'phoneNumber', "width": "15%" },
            {
                data: 'role', "width": "15%", "render": function (data) {
                    return `<span class="badge bg-secondary">${data}</span>`;
                }
            },
            {
                data: 'id', "width": "30%", "render": function (data, type, row) {

                    var today = new Date().getTime();
                    var lockout = new Date(row.lockoutEnd).getTime();
                    var isLocked = lockout > today;

                    return `
                        <div class="d-flex gap-2 justify-content-center">
                            <a onclick="LockUnlock('${data}')" class="btn btn-sm ${isLocked ? 'btn-danger' : 'btn-success'}">
                                <i class="bi bi-${isLocked ? 'lock' : 'unlock'}-fill"></i> ${isLocked ? 'Locked' : 'Unlocked'}
                            </a>
                            <a href="/admin/user/RoleManagement?userId=${data}" class="btn btn-sm btn-outline-secondary">
                                <i class="bi bi-person-badge"></i> Role
                            </a>
                            <a href="/admin/user/ChangePassword?userId=${data}" class="btn btn-sm btn-outline-warning">
                                <i class="bi bi-key-fill"></i> Password
                            </a>
                        </div>
                    `;
                }
            }
        ]
    });
}

function LockUnlock(id) {
    $.ajax({
        type: "POST",
        url: '/admin/user/LockUnlock',
        data: JSON.stringify(id),
        contentType: "application/json",
        success: function (data) {
            if (data.success) {
                toastr.success(data.message);
                userDataTable.ajax.reload();
            }
        }
    });
}