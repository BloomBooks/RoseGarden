# RoseGarden
Download books from OPDS repositories and convert epubs into Bloom source format
# Commands

- **batch** - Batch process books from a catalog, checking which need to be updated, then
    fetching, converting, and uploading. The catalog can be filtered by language, publisher, or
    both.  For example, the following command will update all the French books published by Book
    Dash that are available from the Global Digital library (on a Linux system with several of
    the environment variables described below set appropriately).

    `RoseGarden.exe batch -v -R -s gdl -l French -p "Book Dash" --bloomexe /usr/bin/bloom-alpha --bloomfolder /usr/lib/bloom-desktop "/home/steve/BloomImport/Book Dash/French"`

    The destination directory could also have been expressed as `/home/steve/BloomImport/'$publisher$/$language$'`.
    (Note the single quotes around the dollar signed section to prevent the shell from thinking
    publisher and language are environment variables.)  The actual publisher is substituted for
    `$publisher$` and the actual language is substituted for `$language$` for each book as they
    are processed and the Bloom sources are stored on the local computer.

- **convert** - Convert a single book from epub to Bloom source.

- **fetch** - Fetch a single book or catalog from the OPDS source.  The catalog can be filtered
    by language, publisher, or both.  This filtering affects the book search as well as
    affecting the catalog content stored when only the catalog data is wanted.

- **fixtable** - Fix the table fields for imported books to make up for mistakes in development.
    This command has been needed (and run) once, but it is now a useless relic left in place as
    a framework for possible future hiccups.

- **upload** - Upload a bookshelf of converted books to bloomlibrary.org.  This command uses
    Bloom 4.8 to do the actual uploading.  At the time of writing, this requires the latest alpha
    release, or possibly the latest developer build.  For example, the upload command loosely
    corresponding to the batch command example above would be

    `RoseGarden.exe upload -v -s --bloomexe /usr/bin/bloom-alpha /home/steve/BloomImport`

    The **RoseGarden** upload command queries the parse tables to determine whether an upload is
    needed, overriding (by overwriting) any existing BloomBulkUploadLog.txt file that exists in
    the upload folder.

- **help** - Display more information on a specific command.  For example,

    `RoseGarden.exe help batch`

- **version** - Display version information.

# Environment Variables

- **RoseGardenUserName** - Bloom library username.  This will usually be the special importer
    user.  This can also be provided by a command line option for commands that need it.
- **RoseGardenUserPassword** - password for the given Bloom libary user.  This can also be
    provided by a command line option for commands that need it.
- **RoseGardenEnvironment** - The operational environment: Production, Development, or Test.
    This specifies which parse table url is used by RoseGarden.  The environment can be
    abbreviated to a single letter or spelled out, and can be either uppercase or lowercase.
    This defaults to Development if the environment variable is not set.
- **RoseGardenParseAppIdProd** - The secret key required for accessing the parse tables used by
    bloomlibrary.org.
- **RoseGardenParseAppIdTest** - The secret key required for accessing the parse tables used
    solely for testing.
- **RoseGardenParseAppIdDev** - The secret key required for accessing the parse tables used by
    dev.bloomlibary.org.
- **OPDSTOKEN** - The secret key needed to download books from the current OPDS server.  Some
    sources require this and some don't.  It will be different for every source that requires a
    key token.

These variables can either be mixed case as shown or all uppercase.

For complete generality, there should be environment variables to set the base URL for
production and development uploads and table updating, but for now bloomlibrary.org and
dev.bloomlibrary.org and their related parse table urls are fixed in the code.  This is not
likely to change in the absence of some real need for it.

# Automated Importing Niceties

## Choosing layout

One of the decisions needed on import is whether the book should initially be laid out in
portrait mode or landscape mode.  Traditional books have mostly been published with a portrait
layout where their height is noticeably greater than their width.  With electronic devices, it's
just as easy to lay out the book either way, but one will probably look better based on the
pictures and amount of text in the book.

The current algorithm in **RoseGarden** is rather simplistic.  It scans through the content
pages of the book, noting whether each page has a picture and how much text is on each page.
(This last metric is a simple character count.)  If at least 2/3 of the pages have both a
picture and text, and the maximum amount of text on any page that has a picture is no more than
300 characters, then the book is set to landscape layout on input.  Otherwise, the book is set
to portrait layout.

[It may be worth looking at the image dimensions on each page to see whether the pictures favor
portrait or landscape layout.  It may also be worth using picture metrics to adjust the relative
sizes of the picture and the text areas on the pages.  These additional complications seem
fraught with uncertainty and additional computational complexity, so have not been implemented.]
