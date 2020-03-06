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

## Choosing page layout

One of the decisions needed on import is whether the book should initially be laid out in
portrait mode or landscape mode.  Traditional books have mostly been published with a portrait
layout where their height is noticeably greater than their width.  With electronic devices, it's
just as easy to lay out the book either way, but one will probably look better based on the
pictures and amount of text in the book.

The current algorithm in **RoseGarden** is rather simplistic.

1. The default layout is portrait.
2. *RoseGarden** scans through the content pages of the book, noting whether each page has a
   picture and the image size of that picture.
3. If all of the content pages (or all but one page) contain only a picture, then if the
   pictures average being wider than they are high the layout is changed to landscape.  This
   better fits landscape pictures without text.  In other words, landscape pictures in a pure
   picture book cause a landscape layout of the book.
4. If 2/3 or more of the content pages have both text and a picture, then if the pictures
   average being as high (or higher) as they are wide the layout is changed to landscape.  This
   seems to give a better visual effect.  In other words, square or portrait pictures in a book
   with both a picture and text on most pages cause a landscape layout of the book.

[It may be worthwhile exploring whether adjusting the relative sizes of the picture and text
areas on each page (or for all pages based on some average metric) is feasible based on picture
sizes and amount of text on the pages.  This sounds rather intractible to get reliably good
results programmatically.]

Note that the **convert** command has command line options to explicitly set whether the
book should have a portrait or landscape layout.  If one of these options is set, then the
algorithm described above is ignored.


## Choosing thumbnail / cover images

**RoseGarden** creates thumbnails from the first image on the cover (first) page of the source
epub file.  This follows the logic of **Bloom** in creating thumbnails.  This works well except
for publishers that use a single image for the whole front cover with the title and author
embedded in the image.  African Storybook Initiative is the worst offender we've seen thus far,
with cover images in portrait layout and half the cover image being a solid color as background
for the textual information.  For this publisher, we use the first image from the second (first
content) page of the epub for both the thumbnails and for the cover page image.

For 3Asafeer, **RoseGarden** may end up using the image from the third page for the cover and
thumbnails, since page 3 is actually the first content page.  However, we're currently using the
original cover image for 3Asafeer books despite the embedded text.

There are drawbacks to choosing the image from the first content page.  It's rarely the most
exciting image in the book and thus rarely the one chosen for the cover by the author.  Trying
to find the closest matching image in the book is an interesting AI problem that **RoseGarden**
is unlikely to ever tackle.

## Dealing with books imported before RoseGarden

There are a number of books from Pratham Books and Book Dash (and maybe other publishers) that
have already been converted manually and uploaded to Bloom Library.  To detect this situation,
**RoseGarden** searches for existing books with the same title normalized to all lowercase and
all whitespace converted to single spaces.  If any matching titles are found, false positives
are detected by comparing bookshelf tags as a proxy for publisher.  (If the authors field in the
parse table were filled out, **RoseGarden** would check that too.)  If a match is found that
appears to be valid, the newly imported book is marked in three ways:

1. inCirculation is set false in its entry in the parse books table
2. "todo:check duplicate import" is added to its tags field in the parse books table
3. A new entry is made in the parse relatedBooks table that contains pointers to all the books
   that match title publisher, and author.

