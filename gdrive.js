window.tavenem = window.tavenem || {};
window.tavenem.scop = window.tavenem.scop || {};

const CLIENT_ID = '933297426422-druhuvm4k0a7llu93gunnfll3hvujo7g.apps.googleusercontent.com';

const gapiLoadPromise = new Promise((resolve, reject) => {
    gapiLoadOkay = resolve;
    gapiLoadFail = reject;
});
const gisLoadPromise = new Promise((resolve, reject) => {
    gisLoadOkay = resolve;
    gisLoadFail = reject;
});

let tokenClient;

(async () => {
    // First, load and initialize the gapi.client
    await gapiLoadPromise;
    await new Promise((resolve, reject) => {
        gapi.load('client', { callback: resolve, onerror: reject });
    }).catch(err => console.log(err));
    await gapi.client.init({})
        .catch(err => console.log(err));
    await gapi.client.load('https://www.googleapis.com/discovery/v1/apis/drive/v3/rest')
        .catch(err => console.log(err));

    // Now load the GIS client
    await gisLoadPromise;
    await new Promise((resolve, reject) => {
        try {
            tokenClient = google.accounts.oauth2.initTokenClient({
                client_id: CLIENT_ID,
                scope: 'https://www.googleapis.com/auth/drive.file',
                prompt: 'consent',
                callback: '',  // defined at request time in await/promise scope.
            });
            resolve();
        } catch (err) {
            reject(err);
        }
    }).catch(err => console.log(err));
    window.tavenem.scop._gDriveAvailable = true;
})();
