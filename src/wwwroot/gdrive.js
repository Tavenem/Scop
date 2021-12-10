window.tavenem = window.tavenem || {};
window.tavenem.scop = window.tavenem.scop || {};

function handleDriveClientLoad() {
    gapi.load('client:auth2', initClient);
}

function initClient() {
    gapi.client.init({
        clientId: '933297426422-druhuvm4k0a7llu93gunnfll3hvujo7g.apps.googleusercontent.com',
        discoveryDocs: ["https://www.googleapis.com/discovery/v1/apis/drive/v3/rest"],
        scope: 'https://www.googleapis.com/auth/drive.file'
    }).then(function () {
        gapi.auth2.getAuthInstance().isSignedIn.listen(updateSigninStatus);
        updateSigninStatus(gapi.auth2.getAuthInstance().isSignedIn.get());
    }, function (e) {
        console.error(e);
    });

    const originalTest = RegExp.prototype.test;
    RegExp.prototype.test = function test(v) {
        if (typeof v === 'function' && v.toString().includes('0!==t&&t.addSplice')) {
            return true;
        }
        return originalTest.apply(this, arguments);
    };
}

function updateSigninStatus(isSignedIn) {
    window.tavenem.scop._driveSignedIn = isSignedIn;
    if (isSignedIn) {
        const user = gapi.auth2.getAuthInstance().currentUser.get().getBasicProfile();
        window.tavenem.scop._driveUser = `${user.getName()} (${user.getEmail()})`;
    } else {
        window.tavenem.scop._driveUser = null;
    }
    if (window.tavenem.scop._driveStatusObject) {
        window.tavenem.scop._driveStatusObject.invokeMethodAsync("UpdateDriveStatus", window.tavenem.scop._driveSignedIn, window.tavenem.scop._driveUser);
    }
}