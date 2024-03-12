window.tavenem = window.tavenem || {};
window.tavenem.scop = window.tavenem.scop || {};

export async function driveAuthorize(dotNetObjectRef) {
    window.tavenem.scop._driveStatusObject = dotNetObjectRef;

    if (window.tavenem.scop._gDriveAvailable) {
        return await getTokenCore();
    } else {
        return false;
    }
}

export function driveSignOut(dotNetObjectRef) {
    window.tavenem.scop._driveStatusObject = dotNetObjectRef;
    window.tavenem.scop._driveSignedIn = false;

    const token = gapi.client.getToken();
    if (token !== null) {
        google.accounts.oauth2.revoke(token.access_token);
        gapi.client.setToken('');
    }

    if (window.tavenem.scop._driveStatusObject) {
        window.tavenem.scop._driveStatusObject.invokeMethodAsync("UpdateDriveStatus", false);
    }
}

export function downloadText(filename, text) {
    const blob = new Blob([text], { type: 'application/json' });
    const exportUrl = URL.createObjectURL(blob);
    const a = document.createElement("a");
    document.body.appendChild(a);
    a.href = exportUrl;
    a.download = filename;
    a.target = "_self";
    a.click();
    URL.revokeObjectURL(exportUrl);
}

export function getDriveSignedIn(dotNetObjectRef) {
    if (dotNetObjectRef) {
        window.tavenem.scop._driveStatusObject = dotNetObjectRef;
    }
    return window.tavenem.scop._driveSignedIn || false;
}

export async function loadDriveData(dotNetObjectRef) {
    if (!window.tavenem.scop._driveSignedIn) {
        return;
    }

    if (!window.tavenem.scop._driveFileId) {
        await getDriveFileId();

        if (!window.tavenem.scop._driveFileId) {
            return;
        }
    }

    const request = {
        fileId: window.tavenem.scop._driveFileId,
        alt: 'media'
    };
    gapi.client.drive.files.get(request)
        .then(response => {
            if (response.result) {
                dotNetObjectRef.invokeMethodAsync("DriveFileLoaded", JSON.stringify(response.result));
            }
        }).catch(err => getToken(err))
        .then(retry => gapi.client.drive.files.get(request))
        .then(response => {
            if (response.result) {
                dotNetObjectRef.invokeMethodAsync("DriveFileLoaded", JSON.stringify(response.result));
            }
        }).catch(err => console.log(err));
}

export async function saveDriveData(data) {
    if (!window.tavenem.scop._driveSignedIn) {
        return;
    }

    if (!window.tavenem.scop._driveFileId) {
        await getDriveFileId();
    }

    if (window.tavenem.scop._driveFileId) {
        const url = 'https://www.googleapis.com/upload/drive/v3/files/' + window.tavenem.scop._driveFileId + '?uploadType=media';
        const request = {
            'path': url,
            'method': 'PATCH',
            'body': data
        };
        gapi.client.request(request)
            .then(result => {
                if (result.result) {
                    window.tavenem.scop._driveFileId = result.result.id;
                }
            }).catch(err => getToken(err))
            .then(retry => gapi.client.request(request))
            .then(result => {
                if (result.result) {
                    window.tavenem.scop._driveFileId = result.result.id;
                }
            }).catch(err => console.log(err));
    } else {
        const metadata = {
            name: 'scop.json',
            mimeType: 'application/json'
        };
        const form = new FormData();
        form.append('metadata', new Blob([JSON.stringify(metadata)], { type: 'application/json' }));
        form.append('file', data);
        try {
            let token = gapi.client.getToken();
            if (token == null || token.access_token == null) {
                await getTokenCore();
                token = gapi.client.getToken();
                if (token == null || token.access_token == null) {
                    return;
                }
            }
            const result = await fetch('https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart', {
                method: 'POST',
                headers: new Headers({ Authorization: 'Bearer ' + token.access_token }),
                body: form
            });
            const value = await result.json();
            window.tavenem.scop._driveFileId = value.id;
        } catch (e) {
            console.error(e);
        }
    }
}

async function getDriveFileId() {
    if (!window.tavenem.scop._driveSignedIn) {
        return;
    }

    const request = {
        q: "name='scop.json' and mimeType='application/json'",
        fields: 'files(name, trashed, id)',
        spaces: 'drive'
    };
    gapi.client.drive.files.list(request)
        .then(loadDriveFile)
        .catch(err => getToken(err))
        .then(retry => gapi.client.drive.files.list(request))
        .then(loadDriveFile)
        .catch(err => console.log(err));
}

async function getToken(err) {
    if (err.result.error.code == 401
        || err.result.error.code == 403
        && err.result.error.status == "PERMISSION_DENIED") {
        // The access token is missing, invalid, or expired, prompt for user consent to obtain one.
        await getTokenCore();
    } else {
        // Errors unrelated to authorization: server errors, exceeding quota, bad requests, and so on.
        window.tavenem.scop._driveSignedIn = false;
        if (window.tavenem.scop._driveStatusObject) {
            window.tavenem.scop._driveStatusObject.invokeMethodAsync("UpdateDriveStatus", false);
        }
        throw new Error(err);
    }
}

async function getTokenCore() {
    return await new Promise((resolve, reject) => {
        try {
            // Settle this promise in the response callback for requestAccessToken()
            tokenClient.callback = (resp) => {
                if (resp.error !== undefined) {
                    window.tavenem.scop._driveSignedIn = false;
                    if (window.tavenem.scop._driveStatusObject) {
                        window.tavenem.scop._driveStatusObject.invokeMethodAsync("UpdateDriveStatus", false);
                    }
                    reject(resp);
                }
                window.tavenem.scop._driveSignedIn = true;
                if (window.tavenem.scop._driveStatusObject) {
                    window.tavenem.scop._driveStatusObject.invokeMethodAsync("UpdateDriveStatus", true);
                }
                resolve(resp);
            };
            tokenClient.requestAccessToken();
        } catch (err) {
            window.tavenem.scop._driveSignedIn = false;
            if (window.tavenem.scop._driveStatusObject) {
                window.tavenem.scop._driveStatusObject.invokeMethodAsync("UpdateDriveStatus", false);
            }
            reject(err);
        }
    }).then(resp => {
        return true;
    }).catch(err => {
        console.log(err);
        return false;
    });
}

function loadDriveFile(response) {
    if (response.result && response.result.files && response.result.files.length) {
        let file;
        for (let i = 0; i < response.result.files.length; i++) {
            if (!response.result.files[i].trashed
                && response.result.files[i].name === 'scop.json') {
                file = response.result.files[i];
                break;
            }
        }
        if (file) {
            window.tavenem.scop._driveFileId = file.id;
        }
    } else {
        delete window.tavenem.scop._driveFileId;
    }
}