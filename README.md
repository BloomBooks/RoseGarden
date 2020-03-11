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
   majority of pages have pictures that are wider than they are high, the layout is changed to
   landscape.  This better fits landscape pictures without text.  In other words, landscape
   pictures in a pure picture book cause a landscape layout of the book.
4. If 2/3 or more of the content pages have both text and a picture, then if the majority of
   pages have pictures that are higher than (or as high as) they are wide the layout is changed
   to landscape.  This seems to give a better visual effect.  In other words, square or portrait
   pictures in a book with both a picture and text on most pages cause a landscape layout of the
   book.

[It may be worthwhile exploring whether adjusting the relative sizes of the picture and text
areas on each page (or for all pages based on some average metric) is feasible based on picture
sizes and amount of text on the pages.  This sounds rather intractible to get reliably good
results programmatically.]

Note that the **convert** command has command line options to explicitly set whether the
book should have a portrait or landscape layout.  If one of these options is set, then the
algorithm described above is ignored.

## Parsing ePUB cover pages for information

**RoseGarden** attempts to fill in three pieces of information from the first page ("chapter")
of the ePUB book.  The content of the ePUB first page is scanned to find this information.

1. The cover image, shown on the front cover for most xmatter types, and used for creating
   thumbnail images.
2. The title of the book, shown at the top of the front cover for most xmatter types.
3. The *smallCoverCredits* field shown at the bottom of the front cover for most xmatter types.

The first image found in the body of the first ePUB page file is assumed to be the cover image.
However, some publishers clutter up this image with the title and credits being embedded in the
picture.  See the next section for a discussion of this issue.

If there is no text on the first ePUB page, but only one or more images, then the title and
*smallCoverCredits* information is taken from the ePUB metadata file.  If there is text on the
first ePUB page, then the normal assumption is that the first paragraph contains the title and
following paragraphs contain the credits that go into the *smallCoverCredits* field.  This
simple assumption works for many books.

Some books do not format the text by paragraph, but use <br/> elements to separate raw text
nodes in the body element.  The code in **RoseGarden** allows for this possibility, treating
each successive non-whitespace text node as though it were the content of a paragraph element
and ignoring the <br/> elements otherwise.

Some books (especially those published by 3Asafeer) mark the author and illustrator explicitly
in the first page text by a leading "Author:" or "Illustrator:" tag.  These books sometimes play
other tricks like putting the title after the credits or splitting the title into multiple
paragraphs to format it onto multiple lines.  The code in **RoseGarden** does its best to detect
and handle all of these variations.  At the end of processing, if the title extracted from the
first ePUB page does not match the title in the ePUB metadata when both are normalized for
whitespace, capitalization, and punctuation, then **RoseGarden** uses the title extracted from
the first ePUB page but writes a warning message to the console that the titles do not match.
The differences are usually innocuous, but this can be helpful to find cases where the algorithm
has broken down.

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
to find the closest matching image in the book is an interesting problem that **RoseGarden**
may tackle in the future using perceptual hashes.

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

## Parsing ePUB credit pages for information

Various bits of information about the book need to come from the original OPDS catalog entry for
the book, the ePUB metadata, the content of credit pages in the ePUB book, or possibly from
assumed information about a specific publisher.  This information includes:

1. book copyright holder
2. book copyright date
3. book license
4. image creator(s)
5. image copyright holder(s)
6. image copyright date(s)
7. image license(s)

Books that have been processed through **StoryWeaver** have nicely formatted credit pages that
appear to be generated from a database of credit, copyright, and license information.
**RoseGarden** can parse these pages in either English or French at the moment using text
searching and regular expressions.  (Many non-English books appear to still have English credit
pages.)  Books from other publishers sometimes still have regular patterns on a final credits
page that can be analyzed through a limited set of regular expressions.  For some publishers, we
have to rely on the publisher being the copyright holder for both the text and images in the
book, the copyright year being from the earliest date given in the ePUB metadata, and book and
image license being the same and given in the OPDS catalog entry.  (It still seems incredible
that neither the ePUB metadata nor the OPDS catalog entry has a field for copyright holder!)
