window.tavenem = window.tavenem || {};
window.tavenem.scop = window.tavenem.scop || {};

export function addColorSchemeListener(dotNetObjectRef, id) {
    window.tavenem.scop._colorSchemeListeners = window.tavenem.scop._colorSchemeListeners || {};
    window.tavenem.scop._colorSchemeListeners[id] = dotNetObjectRef;
}

export function addTimelineCategory(name) {
    if (window.tavenem.scop._timelineGroupsDataSet) {
        const next = window.tavenem.scop._timelineGroupsDataSet.getIds().length + 1;
        const group = { id: next, content: name, value: next };
        window.tavenem.scop._timelineGroupsDataSet.add([group]);
    } else {
        const group = { id: 1, content: name, value: 1 };
        window.tavenem.scop._timelineGroupsDataSet = new vis.DataSet([group]);
        window.tavenem.scop._timelineGroupsDataSet.on('*', function (event, properties) {
            if (properties.items
                && properties.items.length) {
                let item = window.tavenem.scop._timelineGroupsDataSet.get(properties.items[0]);
                if (event === 'add') {
                    window.tavenem.scop._timelineRef.invokeMethodAsync("AddCategory", item);
                } else if (event === 'remove') {
                    window.tavenem.scop._timelineRef.invokeMethodAsync("RemoveCategory", properties.items[0]);
                } else if (event === 'update') {
                    window.tavenem.scop._timelineRef.invokeMethodAsync("UpdateCategory", item);
                }
            }
        });
        window.tavenem.scop._timelineDataSet.update(
            window.tavenem.scop._timelineDataSet.getIds().map(function (e) {
                return {
                    id: e,
                    group: 1,
                };
            }));
        if (window.tavenem.scop._timeline) {
            window.tavenem.scop._timeline.setGroups(window.tavenem.scop._timelineGroupsDataSet);
            window.tavenem.scop._timelineRef.invokeMethodAsync("AddCategory", group);
        }
    }
}

export function disposeColorSchemeListener(id) {
    if (window.tavenem.scop._colorSchemeListeners) {
        delete window.tavenem.scop._colorSchemeListeners[id];
    }
}

export function disposeTimeline() {
    if (window.tavenem.scop._timeline) {
        window.tavenem.scop._timeline.destroy();
    }
}

export function driveAuthorize(dotNetObjectRef) {
    window.tavenem.scop._driveStatusObject = dotNetObjectRef;
    gapi.auth2.getAuthInstance().signIn();
}

export function driveSignout(dotNetObjectRef) {
    window.tavenem.scop._driveStatusObject = dotNetObjectRef;
    gapi.auth2.getAuthInstance().signOut();
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

export function getDriveUser(dotNetObjectRef) {
    if (dotNetObjectRef) {
        window.tavenem.scop._driveStatusObject = dotNetObjectRef;
    }
    return window.tavenem.scop._driveUser || null;
}

export function getPreferredColorScheme() {
    if (window.tavenem.scop._manualColorTheme
        && window.tavenem.scop._theme) {
        return window.tavenem.scop._theme;
    }
    if (window.matchMedia) {
        if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 2;
        } else {
            return 1;
        }
    }
    return 1;
}

export function initializeTimeline(dotNetObjectRef, elementId, now, events, categories) {
    window.tavenem.scop._timelineRef = dotNetObjectRef;

    if (window.tavenem.scop._timelineDataSet) {
        window.tavenem.scop._timelineDataSet.clear();
        if (events) {
            window.tavenem.scop._timelineDataSet.update(events);
        }
    } else {
        window.tavenem.scop._timelineDataSet = new vis.DataSet(events || []);
        window.tavenem.scop._timelineDataSet.on('*', function (event, properties) {
            if (properties.items
                && properties.items.length) {
                let item = window.tavenem.scop._timelineDataSet.get(properties.items[0]);
                if (event === 'add') {
                    window.tavenem.scop._timelineRef.invokeMethodAsync("AddEvent", item);
                } else if (event === 'remove') {
                    window.tavenem.scop._timelineRef.invokeMethodAsync("RemoveEvent", properties.items[0]);
                } else if (event === 'update') {
                    window.tavenem.scop._timelineRef.invokeMethodAsync("UpdateEvent", item);
                }
            }
        });
    }

    if (window.tavenem.scop._timelineGroupsDataSet) {
        window.tavenem.scop._timelineGroupsDataSet.clear();
    }
    if (categories && categories.length) {
        if (window.tavenem.scop._timelineGroupsDataSet) {
            window.tavenem.scop._timelineGroupsDataSet.update(categories);
        } else {
            window.tavenem.scop._timelineGroupsDataSet = new vis.DataSet(categories || []);
            window.tavenem.scop._timelineGroupsDataSet.on('*', function (event, properties) {
                if (properties.items
                    && properties.items.length) {
                    let item = window.tavenem.scop._timelineGroupsDataSet.get(properties.items[0]);
                    if (event === 'add') {
                        window.tavenem.scop._timelineRef.invokeMethodAsync("AddCategory", item);
                    } else if (event === 'remove') {
                        window.tavenem.scop._timelineRef.invokeMethodAsync("RemoveCategory", properties.items[0]);
                    } else if (event === 'update') {
                        window.tavenem.scop._timelineRef.invokeMethodAsync("UpdateCategory", item);
                    }
                }
            });
        }
    } else {
        window.tavenem.scop._timelineGroupsDataSet = null;
    }

    window.tavenem.scop._timeline = new vis.Timeline(
        document.getElementById(elementId),
        window.tavenem.scop._timelineDataSet,
        window.tavenem.scop._timelineGroupsDataSet,
        {
            editable: true,
            groupEditable: true,
            groupOrder: function (a, b) {
                return a.value - b.value;
            },
            groupOrderSwap: function (a, b, groups) {
                let v = a.value;
                a.value = b.value;
                b.value = v;
            },
            groupTemplate: function (group) {
                if (!group) {
                    return null;
                }
                let container = document.createElement('div');
                let label = document.createElement('span');
                label.innerHTML = group.content || '';
                container.appendChild(label);
                let deleteButton = document.createElement('button');
                deleteButton.classList = "mud-button-root mud-icon-button mud-icon-button-color-error mud-ripple mud-ripple-icon ms-1";
                deleteButton.type = 'button';
                deleteButton.addEventListener('click', function () {
                    window.tavenem.scop._timelineGroupsDataSet.remove(group.id);
                });
                container.appendChild(deleteButton);
                let iconLabel = document.createElement('span');
                iconLabel.classList = "mud-icon-button-label";
                iconLabel.textContent = "×";
                deleteButton.appendChild(iconLabel);
                return container;
            },
            itemsAlwaysDraggable: true,
            cluster: {
                titleTemplate: "{count} events"
            }
        }
    );
    window.tavenem.scop._timeline.on('select', function (properties) {
        if (properties.items
            && properties.items.length) {
            let item = window.tavenem.scop._timelineDataSet.get(properties.items[0]);
            window.tavenem.scop._timelineRef.invokeMethodAsync("SelectEvent", item);
        }
    });
    window.tavenem.scop._timeline.on('timechanged', function (properties) {
        if (properties.time) {
            window.tavenem.scop._timelineRef.invokeMethodAsync("OnNowChanged", properties.time);
        }
    });
    if (now) {
        window.tavenem.scop._timeline.hasNow = true;
        window.tavenem.scop._timeline.addCustomTime(now, 1);
        window.tavenem.scop._timeline.setCustomTimeMarker("Now", 1);
    }
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

    try {
        const response = await gapi.client.drive.files.get({
            fileId: window.tavenem.scop._driveFileId,
            alt: 'media'
        });
        if (response.result) {
            dotNetObjectRef.invokeMethodAsync("DriveFileLoaded", JSON.stringify(response.result));
        }
    } catch (e) {
        console.error(e);
    }
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
        try {
            const result = await gapi.client.request({
                'path': url,
                'method': 'PATCH',
                'body': data
            });
            if (result.result) {
                window.tavenem.scop._driveFileId = result.result.id;
            }
        } catch (e) {
            console.error(e);
        }
    } else {
        const metadata = {
            name: 'scop.json',
            mimeType: 'application/json'
        };
        const form = new FormData();
        form.append('metadata', new Blob([JSON.stringify(metadata)], { type: 'application/json' }));
        form.append('file', data);
        try {
            var accessToken = gapi.auth.getToken().access_token;
            const result = await fetch('https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart', {
                method: 'POST',
                headers: new Headers({ Authorization: 'Bearer ' + accessToken }),
                body: form
            });
            const value = await result.json();
            window.tavenem.scop._driveFileId = value.id;
        } catch (e) {
            console.error(e);
        }
    }
}

export function scrollToId(elementId) {
    const element = document.getElementById(elementId);
    if (element instanceof HTMLElement) {
        element.scrollIntoView({
            behavior: "smooth",
            block: "start",
            inline: "nearest"
        });
    }
}

export function setColorScheme(theme, manual) {
    if (manual) {
        window.tavenem.scop._manualColorTheme = (theme !== 0);
    } else if (window.tavenem.scop._manualColorTheme) {
        return;
    }
    window.tavenem.scop._theme = theme;

    if (theme === 0) {
        theme = getPreferredColorScheme();
    }
    const root = document.documentElement;
    if (theme === 2) { // dark
        root.style.setProperty('--body-bg-color', "#222");
        root.style.setProperty('--body-color', '#ffffffb2');
        root.style.setProperty('--vis-color', "#999");
        root.style.setProperty('--valid-modified-outline', "1px solid #26b050");
    } else { // light
        root.style.setProperty('--body-bg-color', 'white');
        root.style.setProperty('--body-color', 'black');
        root.style.setProperty('--vis-color', "#111");
        root.style.setProperty('--valid-modified-outline', "1px solid green");
    }

    if (window.tavenem.scop._colorSchemeListeners) {
        for (const id in window.tavenem.scop._colorSchemeListeners) {
            const ref = window.tavenem.scop._colorSchemeListeners[id];
            if (ref) {
                ref.invokeMethodAsync("UpdateComponentTheme", theme);
            }
        }
    }
}

export function setCurrentTime(now) {
    if (window.tavenem.scop._timeline) {
        if (window.tavenem.scop._timeline.hasNow) {
            window.tavenem.scop._timeline.removeCustomTime(1);
            window.tavenem.scop._timeline.hasNow = false;
        }
        if (now) {
            window.tavenem.scop._timeline.addCustomTime(now, 1);
            window.tavenem.scop._timeline.setCustomTimeMarker("Now", 1);
            window.tavenem.scop._timeline.hasNow = true;
        }
    }
}

export function setTimelineCategories(categories) {
    if (window.tavenem.scop._timelineGroupsDataSet) {
        window.tavenem.scop._timelineGroupsDataSet.clear();
        if (categories) {
            window.tavenem.scop._timelineGroupsDataSet.update(categories);
        }
    }
}

export function setTimelineEvents(events) {
    if (window.tavenem.scop._timelineDataSet) {
        window.tavenem.scop._timelineDataSet.clear();
        if (events) {
            window.tavenem.scop._timelineDataSet.update(events);
        }
    }
}

async function getDriveFileId() {
    if (!window.tavenem.scop._driveSignedIn) {
        return;
    }

    try {
        const response = await gapi.client.drive.files.list({
            q: "name='scop.json' and mimeType='application/json'",
            fields: 'files(name, trashed, id)',
            spaces: 'drive'
        });
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
    } catch (e) {
        console.error(e);
    }
}

if (window.matchMedia) {
    const colorSchemeQuery = window.matchMedia('(prefers-color-scheme: dark)');
    colorSchemeQuery.addEventListener('change', setColorScheme(getPreferredColorScheme()));
    const currentScheme = getPreferredColorScheme();
    if (currentScheme === 2) {
        setColorScheme(currentScheme);
    }
}