// requeire to have window.isAdmin
var nonAdminAction = function (action) {
    if (window.isAdmin != null && window.isAdmin == false) {
        action();
    }
}