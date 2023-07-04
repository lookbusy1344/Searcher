# Test commands and expected results

These are all automated in the TestSearcher project (xUnit). They run against the sample files in the TestDocs folder.

## General tests for pdf, docx and txt

### searcher.exe -s "terrors of the earth"
Expected: the 3 king lear files and 'King Lear pdf.zip' (a pdf inside a zip) and 'Lear and Macbeth docx.zip' (a docx inside a zip)

### searcher.exe -s "it is the east"
Expected: the 3 Romeo files, and 'Macbeth and Romeo txt.zip'

## Test for basic zip

### searcher.exe -s "poor player That struts"
Expected: the 3 Macbeth files, 'Macbeth and Romeo txt.zip' and 'Lear and Macbeth docx.zip'

## Test for nested zips

### searcher.exe -s "brown fox"
Expected: 'Nested zip brown fox.zip'

## Basic txt result

### searcher.exe -s "this day"
Expected: 'Henry V.txt'

## Filtering on 2 globs

### searcher.exe -s "terrors of the earth" -p \*.pdf,\*.txt
Expected: the 2 king lear files

## Using the -z flag to look inside zips

### searcher.exe -s "it is the east" -p \*.docx -z
Expected: 'Romeo and Juliet.docx'. The zip containing the txt file is not returned.

### searcher.exe -s "it is the east" -p \*.docx,\*.txt -z
Expected: the docx, the txt, and 'Macbeth and Romeo txt.zip'

## Case-sensitive

### searcher.exe -s "Having some BUSINESS" -p *.txt
Expected: Romeo and Juliet.txt, the case is wrong but default search is insensitive

### searcher.exe -s "Having some BUSINESS" -p *.txt -c
Expected: none, case-sensitive search

### searcher.exe -s "Having some business" -p *.docx -c
Expected: Romeo and Juliet.docx, now we have the right case


## No match

### searcher.exe -s "summer"
Expected: none, this text does not exist

### searcher.exe -s "terrors of the earth" -p \*.log,\*.x
Expected: none, although the text exists, the globs are wrong
