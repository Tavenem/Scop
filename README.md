![build](https://img.shields.io/github/workflow/status/Tavenem/Scop/publish/main)

Scop
==

Scop is a story organization tool for authors. It provides hierarchical note management, rich character descriptions, and a timeline feature.

## Installation

Scop is available at https://tavenem.com/Scop, and can optionally be installed as a [progressive web app](https://en.wikipedia.org/wiki/Progressive_web_application) for offline use.

## About

Scop is a free story organization tool.

Add rich text notes, and nest notes inside them. You can nest notes within each other to any depth.

Add notes about characters with structured information such as name, age, gender, ethnicity, traits, and relationships.

Create a timeline of events, and keep track of the current date and time in your story's narrative.

Work online or offline, and synchronize your data between devices.

## How to Use Scop

To get started, first go the Stories page and add a story.

Once you have a story open, you'll see the note tree. At first, the only items will be the Timeline and the blank line where you can add a new note.

Type a name for your first note in the blank line to get started.
Press <kbd>Enter</kbd> or click/tap outside the box to add the new note.

### Notes

Once you have a note selected, you'll see the text editor.
This editor supports rich text formatting, such as bold and italic text, headings, tables, etc.
You can work with it in [WYSIWYG](https://en.wikipedia.org/wiki/WYSIWYG") mode (like a typical word processor), or you can toggle [Markdown](https://en.wikipedia.org/wiki/Markdown) mode to work directly with the formatting code.

### Characters

To create a character note, click the icon to the left of a note's name, and select the person icon.
Any notes you already have will be preserved.
Character notes still have a text editor for free-form notes, in addition to their structured character data.
You can change a character note back to a normal note by toggling back to the note icon.
Changing a character to a normal note preserves the character information by writing it into the text content of the note in a summary style.

Note that a converted character summary doesn't convert back:
if you convert a note from a character to a normal note, than back to a character again, the structured character data will remain in summary form in the text part of the note.

### Saving

Your stories are automatically saved to your browser's local storage.
Data is saved any time you make a change, so you don't need to manually save at any point.
Your browser's local storage is also available offline, so saving and loading will keep working even if your internet stops working while you're using Scop.
Local browser storage is not permanent, though.
Clearing your browser data may erase this information, and it doesn't synchronize between different devices, or even different browsers on the same device.

To synchronize your Scop data between all your devices, and make sure it doesn't get accidentally deleted, go to the Manage Data page and sign into your Google Drive™ account.
Even if you have never used Google Drive before, if you have a Gmail address you also have a Google Drive account.

If you *don't* have a Google account (or if you want to create a new one for your Scop story data), you can [sign up for one here](https://accounts.google.com/signup/v2/webcreateaccount?hl=en&flowName=GlifWebSignIn&flowEntry=SignUp).
It's free and easy to do.
Once you sign into a new or existing Google Drive account with Scop, your stories will be saved to your Google Drive account automatically.
Sign into Google Drive from Scop on any browser and device to see all your story data.

Be careful not to create new, different story content on a separate device *before* signing into a Google Drive account which you are already using with Scop.
This would cause the new story data to overwrite your existing data.
If you *do* accidentally overwrite your Google Drive data, Google Drive has built-in functionality to track changes, and revert to previous versions.
Your data is saved in Google Drive as a file called "scop.json."

Scop will continue to save your data locally in your browser, even if you are signed into a Google Drive account.
If you ever sign out of Google Drive, either deliberately or because your sign-in expires, your local data should be up to date on the latest device you used with Scop.
Sign into Google Drive again to resume saving to your account.

### Working Offline

For a true offline experience, you can install Scop as a [progressive web app](https://en.wikipedia.org/wiki/Progressive_web_application) (PWA).
That means Scop will be availble on your device like an installed program, without opening your browser or even being online.
When you *are* online, your data will sync as usual.
Not every browser supports PWAs on every platform, but most do.
To install Scop as a PWA, look near the top right of your browser.
If you don't see the option, try searching for "how to install a progressive web app on {insert your browser and operating system here}" to find the latest instructions for your setup.

While working with Scop as a PWA, if you are offline (with no internet access), Scop will only save your data locally, even if you normally sync to a Google Drive account.
Once your device is back online, open Scop to sync your changes with Google Drive.
Be careful not to make offline changes with Scop on different devices, because the last one to go online will overwrite anything saved to Google Drive.
If you accidentally overwrite changes made with a different device, you can use Google Drive's document history features to recover your lost data.

## Characters

Character notes can have the same rich text information as normal notes, plus a set of structured data.

### Name

Characters can have an unlimited number of given and surnames.

Given names can include the "first" name and any number of "middle" names, or maybe one or more nicknames.

A character's surnames can by hyphenated, or separate.

A character can also have an optional title (e.g. Mr., Mrs.), and suffix (e.g. Jr., esq.).

A character's name can also be picked at random.
You can randomly pick the character's entire name, or you can randomize each one of the character's given or surnames at a time.
If the character has been assigned a specific ethnicity (or ethnicities), Scop will try to pick an appropriate name from a list of the common names for that culture.
Where data was available, these lists are weighted according to the popularity of the names, so common names will be selected more often than unusual ones.

### Age

Characters can be assigned a birthdate, or given a specific age.

The current age of a character with a birthdate is determined by the "current time" in your story.
You can set the current time in the special Timeline note.
If you *don't* set the current time, it defaults to the actual current date and time.

A character without a birthdate can be assigned a specific age in years, months, and days.
This may be more convenient if you tend to write stories without a specific current time, or stories which don't use the Gregorian calendar.

A character's age can also be picked at random.
If your story has a current time set, this will give the character a random birthdate.
If not, it will assign a fixed age.
If the character has been assigned relationships with other characters, Scop will try its best to pick an appropriate age based on the age of those characters, to avoid improbable values (like a child older than its parent, for instance).

### Gender

A character can be assigned a gender, and pronouns. These can also be assigned randomly.

The gender assigned to a character is flexible: you can enter any text you like (although some common options are provided as autocomplete suggestions).

The pronouns you give a character, however, must be selected from a short list.
This restriction helps Scop determine the correct terms for relationships, and pick appropriate random names.

When you select certain genders from the autocomplete list, the usual pronouns for that gender are automatically selected.
You are free to override this with a different choice, however.

### Ethnicity

Characters can be assigned any number of ethnicities.

The list of included ethnicities is based purely on Scop's available data for random name selection.
It is by no means intended to reflect the complete spectrum of ethnicities possible, and is woefully inadequate in many areas due to a lack of reliable name data.

You can add any number of custom ethnicities, either as child entries under an existing ethnicity, or as completely new values.
Scop will not be able to generate random names for custom ethnicities.
If you add your new ethnicity as a child of an existing ethnicity, Scop will use the parent entry when picking random names.
If you add a completely new ethnicity, Scop will default to the list of U.S. names.

### Traits

A tree of character traits is available to assign, either by hand or randomly.

The traits include both physical and personality traits, and also professions.

You can also add your own custom traits, either as child entries under an existing trait, or as completely new values.

Random trait selection can be a complex process. When you create a custom trait, you can edit its properties to determine how likely it is to be selected during random generation.
You can also specify whether there are any conditions that modify its usual chances of being selected.

### Relationships

Characters can be assigned relationships.
Their relationships can be with other defined characters, or with arbitrary people who don't have a specific character note.

A relationship specifies the related character (either a character note, or any arbitrary name), the type of relationship (e.g. parent, spouse, sibling), the relationship name (e.g. mother, husband, sister), and optionally an inverse type.

If the related character is a defined character note, and the relationship type you pick is one of the common familial relationship types in the autocomplete list, Scop can usually determine the relationship name automatically, based on that character's assigned gender pronouns.
When you use a custom type, you may need to specify the relationship name manually.
It defaults to the same text used for the type, which is fine in most cases (e.g. "friend").

When you assign a character a relationship with another defined character note, Scop will also show a reciprocal relationship on the other character.
For example, if you assign a relationship to character named Sue, and specify another chartacter named Mary as Sue's mother, when you look at Mary's own note you'll see a read-only relationship that shows Sue as her daughter.

Scop can identify the reciprocal relationship for the common familial relationship types in the autocomplete list.
It knows that the inverse of "parent" is "child," etc.
For other relationships, it assumes the inverse is the same as the original relationship type.
For instance, the inverse of "friend" is also "friend."
When the inverse isn't supplied automatically by Scop, and isn't the same as the original (e.g. "employer" vs. "employee"), you can specify the inverse by hand.

## The Timeline

Each story has a special Timeline note (at the top).

The timeline can be zoomed in and out to show more or less detail.

You can double-tap to add an event with a specific time, or hold and drag to create an event that goes on for a period of time.
Events that have been added to the timeline can be dragged to change their date/time.
They will snap to the nearest tickmark displayed on the timeline.
You can zoom in or out to change the spacing of the tickmarks.

If you zoom out far enough that events get pushed too close together, they'll merge into a combined marker that identifies how many events take place at that point in the timeline. Zoom back in to see the events separately again.

When you select an event, a rich text editor appears where you can take notes about that event.
You can also fine-tune the exact date and time of the event (both start and end, for events that span a range of time).

You can also set your story's "current" time. This might represent the date and time as of the story's beginnning, during the chapter where you're currently working, or any other point you want.
When you add a current time, it defaults to the actual current time and date.
After setting a current time for your story, it appears on the timeline as a vertical line, which can be dragged along the timeline to update the value.

## Roadmap

No specific feature updates are planned for Scop, but changes and bugfixes are always possible.

## Contributing

Contributions are always welcome. Please carefully read the [contributing](docs/CONTRIBUTING.md) document to learn more before submitting issues or pull requests.

## Code of conduct

Please read the [code of conduct](docs/CODE_OF_CONDUCT.md) before engaging with our community, including but not limited to submitting or replying to an issue or pull request.