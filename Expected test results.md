# Test commands and expected results

## General tests for pdf, docx and txt

### searcher.exe -s "terrors of the earth"
Expected: the 3 king lear files

### searcher.exe -s "it is the east"
Expected: the 3 Romeo files, and 'Macbeth and Romeo txt.zip'

## Test for basic zip

### searcher.exe -s "poor player That struts"
Expected: the 3 Macbeth files, and 'Macbeth and Romeo txt.zip'

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

## No match

### searcher.exe -s "summer"
Expected: none
