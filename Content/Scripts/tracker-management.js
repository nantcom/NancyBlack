// requeire to have window.isAdmin
var nonAdminAction = function (action) {
    if (window.isAdmin == false) {
        action();
    }
}