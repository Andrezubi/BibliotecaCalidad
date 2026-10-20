# (Example that has already been implemented) UpdateAsync
@startuml
title Grafo Function UpdateAsync

skinparam usecase {
  BackgroundColor White
  BorderColor Black
  BorderThickness 2
  FontColor #32c9a6
  FontStyle bold
  FontSize 18
}
skinparam ArrowColor Black
skinparam shadowing false

usecase "I" as NI
usecase "1" as N1
usecase "2" as N2
usecase "3" as N3
usecase "4" as N4
usecase "5" as N5
usecase "6" as N6
usecase "7" as N7
usecase "8" as N8
usecase "9" as N9
usecase "10" as N10
usecase "11" as N11
usecase "12" as N12
usecase "F" as NF

NI --> N1
N1 --> N2
N2 --> N3
N3 --> NF : True
N3 --> N4 : False
N4 --> NF : True
N4 --> N5 : False
N5 --> N6
N6 --> N7
N7 --> N8
N8 --> N9
N9 --> N10
N10 --> N11
N11 --> N12
N12 --> NF

legend right
| Nodo | Código |
| I | Inicio |
| 1 | ThrowIfNull(dto) |
| 2 | GetByIdAsync(id) |
| 3 | book == null |
| 4 | book.IsActive != true |
| 5 | NormalizeAuthorIds / NormalizeCategoryIds |
| 6 | ValidateAuthorsAsync |
| 7 | ValidateCategoriesAsync |
| 8 | ValidateCoverFile |
| 9 | ValidateUpdateIsbnAsync |
| 10 | UpdateBookEntity / BookValidator.Validate |
| 11 | UpdateBookTransactionAsync |
| 12 | DeleteCoverImage |
| F | MapToDtoAsync / return |
endlegend

@enduml



Nro	Camino	Entrada	Salida
1	I,1,2,3,F	id=1, dto=ValidDto(), repository returns null	null. The book does not exist, so execution returns immediately.
2	I,1,2,3,4,F	id=1, dto=ValidDto(), repository returns a book with IsActive=false	null. The book exists but is inactive, so execution returns immediately.
3	I,1,2,3,4,5,6,7,8,9,10,11,12,F	id=1, dto=ValidDto(), repository returns an active book, all validations succeed, transaction succeeds	A non-null BookDto representing the updated book, assuming mapping succeeds.





# UpdateBookTransaction()
@startuml
title Grafo Function UpdateBookTransactionAsync

skinparam usecase {
  BackgroundColor White
  BorderColor Black
  BorderThickness 2
  FontColor #32c9a6
  FontStyle bold
  FontSize 18
}
skinparam ArrowColor Black
skinparam shadowing false

usecase "I" as NI
usecase "1" as N1
usecase "2" as N2
usecase "3" as N3
usecase "4" as N4
usecase "5" as N5
usecase "6" as N6
usecase "7" as N7
usecase "8" as N8
usecase "9" as N9
usecase "10" as N10
usecase "11" as N11
usecase "12" as N12
usecase "13" as N13
usecase "14" as N14
usecase "F" as NF

NI --> N1
N1 --> N2
N1 --> N12 : Exception
N2 --> N3 : True
N2 --> N5 : False
N3 --> N4
N4 --> N5
N4 --> N12 : Exception
N5 --> N6
N5 --> N12 : Exception
N6 --> N7 : True
N6 --> N7 : False
N7 --> N8
N7 --> N12 : Exception
N8 --> N9 : True
N8 --> N9 : False
N9 --> N10
N9 --> N12 : Exception
N10 --> N11
N10 --> N12 : Exception
N11 --> NF
N12 --> N13
N13 --> N14
N14 --> NF

legend right
| Nodo | Código |
| I | Initialize path and begin transaction |
| 1 | SaveCoverAsync |
| 2 | IsNullOrWhiteSpace(newCoverPath) condition |
| 3 | Assign book.CoverImage |
| 4 | BookValidator.Validate |
| 5 | UpdateAsync(book) |
| 6 | dto.UserId != null (author argument) |
| 7 | UpdateAuthorsAsync |
| 8 | dto.UserId != null (category argument) |
| 9 | UpdateCategoriesAsync |
| 10 | CommitAsync |
| 11 | Return newCoverPath |
| 12 | RollbackAsync |
| 13 | DeleteCoverImage |
| 14 | Rethrow exception |
| F | End / return or exception propagation |
endlegend

@enduml


Nro	Camino	Entrada	Salida
1	I,1,2,5,6,7,8,9,10,11,F	dto.CoverImage=null, dto.UserId=null, SaveCoverAsync returns null, all repository operations succeed	Returns null. No cover validation or cover assignment occurs; both update methods receive a null user ID. Transaction commits.
2	I,1,2,5,6,7,8,9,10,11,F	dto.CoverImage=null, dto.UserId=12, SaveCoverAsync returns null, all repository operations succeed	Returns null. No cover validation or cover assignment occurs; both update methods receive user ID 12. Transaction commits.
3	I,1,2,3,4,5,6,7,8,9,10,11,F	dto.CoverImage="cover.jpg", dto.UserId=null, SaveCoverAsync returns "covers/new.jpg", all operations succeed	Returns "covers/new.jpg". Cover is assigned and validated; both update methods receive a null user ID. Transaction commits.
4	I,1,2,3,4,5,6,7,8,9,10,11,F	dto.CoverImage="cover.jpg", dto.UserId=12, SaveCoverAsync returns "covers/new.jpg", all operations succeed	Returns "covers/new.jpg". Cover is assigned and validated; both update methods receive user ID 12. Transaction commits.
5	I,1,12,13,14,F	book=validBook, dto=validDto, SaveCoverAsync throws IOException	RollbackAsync executes, DeleteCoverImage(null) executes, and the original IOException is rethrown.
6	I,1,2,3,4,12,13,14,F	book=validBook, dto.CoverImage="cover.jpg", SaveCoverAsync returns "covers/new.jpg", BookValidator.Validate throws ValidationException	Rollback executes, DeleteCoverImage("covers/new.jpg") executes, and the validation exception is rethrown.
7	I,1,2,5,12,13,14,F	book=validBook, dto.CoverImage=null, SaveCoverAsync returns null, _bookRepository.UpdateAsync throws InvalidOperationException	Rollback executes, DeleteCoverImage(null) executes, and the original exception is rethrown.
8	I,1,2,5,6,7,12,13,14,F	book=validBook, dto.UserId=12, cover save and book update succeed, UpdateAuthorsAsync throws InvalidOperationException	Rollback executes, DeleteCoverImage(null) executes, and the exception is rethrown.
9	I,1,2,5,6,7,8,9,12,13,14,F	book=validBook, dto.UserId=12, cover save and author update succeed, UpdateCategoriesAsync throws InvalidOperationException	Rollback executes, DeleteCoverImage(null) executes, and the exception is rethrown.
10	I,1,2,5,6,7,8,9,10,12,13,14,F	book=validBook, dto.CoverImage=null, save and repository updates succeed, CommitAsync throws InvalidOperationException	Rollback is attempted, DeleteCoverImage(null) executes if rollback succeeds, and the commit exception is rethrown.

# AddCopyAynsc()

@startuml
title Grafo Function AddCopyAsync

skinparam usecase {
  BackgroundColor White
  BorderColor Black
  BorderThickness 2
  FontColor #32c9a6
  FontStyle bold
  FontSize 18
}
skinparam ArrowColor Black
skinparam shadowing false

usecase "I" as NI
usecase "1" as N1
usecase "2" as N2
usecase "3" as N3
usecase "4" as N4
usecase "5" as N5
usecase "6" as N6
usecase "7" as N7
usecase "8" as N8
usecase "9" as N9
usecase "10" as N10
usecase "11" as N11
usecase "12" as N12
usecase "13" as N13
usecase "F" as NF

NI --> N1
N1 --> N2 : True
N1 --> N3 : False
N2 --> NF
N3 --> N4
N4 --> N5 : True
N4 --> N6 : False
N5 --> NF
N6 --> N7
N7 --> N9 : True
N7 --> N8 : False
N8 --> N9 : True
N8 --> N10 : False
N9 --> NF
N10 --> N11 : True
N10 --> N12 : False
N11 --> NF
N12 --> N13
N13 --> NF

legend right
| Nodo | Código |
| I | Initialize parameters |
| 1 | IsNullOrWhiteSpace(internalCode) |
| 2 | Return required-code error |
| 3 | Trim internalCode |
| 4 | internalCode.Length > 50 |
| 5 | Return length error |
| 6 | GetByIdAsync(bookId) |
| 7 | book == null |
| 8 | book.IsActive != true |
| 9 | Return book-not-found error |
| 10 | InternalCodeExistsAsync(internalCode) |
| 11 | Return duplicate-code error |
| 12 | Create Copy |
| 13 | AddCopyAsync(copy) |
| F | Return null / end |
endlegend

@enduml
Nro	Camino	Entrada	Salida
1	I,1,2,F	bookId=1, internalCode="   "	"El código interno es obligatorio."
2	I,1,3,4,5,F	bookId=1, internalCode="ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789012345678901234567890" (56 characters)	"El código interno no puede superar los 50 caracteres."
3	I,1,3,4,6,7,9,F	bookId=1, internalCode="COPY-001", repository returns null for the book	"El libro no existe."
4	I,1,3,4,6,7,8,9,F	bookId=1, internalCode="COPY-001", repository returns a book with IsActive=false	"El libro no existe."
5	I,1,3,4,6,7,8,10,11,F	bookId=1, internalCode="COPY-001", repository returns an active book and InternalCodeExistsAsync returns true	"Ya existe una copia con ese código interno."
6	I,1,3,4,6,7,8,10,12,13,F	bookId=1, internalCode=" COPY-001 ", repository returns an active book and InternalCodeExistsAsync("COPY-001") returns false	null. A copy is created with BookId=1, InternalCode="COPY-001", StatusId=1, and CreatedAt=DateTime.Now; AddCopyAsync is called.



# ValidateCoverFile() 
@startuml
title Grafo Function ValidateCoverFile

skinparam usecase {
  BackgroundColor White
  BorderColor Black
  BorderThickness 2
  FontColor #32c9a6
  FontStyle bold
  FontSize 18
}
skinparam ArrowColor Black
skinparam shadowing false

usecase "I" as NI
usecase "1" as N1
usecase "2" as N2
usecase "3" as N3
usecase "4" as N4
usecase "5" as N5
usecase "6" as N6
usecase "7" as N7
usecase "8" as N8
usecase "9" as N9
usecase "10" as N10
usecase "11" as N11
usecase "12" as N12
usecase "F" as NF

NI --> N1
N1 --> N2 : True
N1 --> N3 : False
N2 --> NF
N3 --> N4 : True
N3 --> N5 : False
N4 --> NF
N5 --> N6 : True
N5 --> N7 : False
N6 --> NF
N7 --> N8
N8 --> N9 : True
N8 --> N10 : False
N9 --> NF
N10 --> NF : False
N10 --> N11 : True
N11 --> N12 : True
N11 --> NF : False
N12 --> NF

legend right
| Nodo | Código |
| I | Receive file |
| 1 | file == null |
| 2 | Return |
| 3 | file.Length <= 0 |
| 4 | Throw empty-file exception |
| 5 | file.Length > MaxFileSize |
| 6 | Throw oversized-file exception |
| 7 | Extract and normalize extension |
| 8 | Extension is not allowed |
| 9 | Throw invalid-extension exception |
| 10 | ContentType is not null/whitespace |
| 11 | ContentType is not allowed |
| 12 | Throw invalid-content-type exception |
| F | End / return / exception propagation |
endlegend

@enduml

Nro	Camino	Entrada	Salida
1	I,1,2,F	file=null	Returns normally without throwing an exception.
2	I,1,3,4,F	file.Length=0, file.FileName="cover.jpg", file.ContentType="image/jpeg"	Throws ArgumentException: "La imagen de portada está vacía."
3	I,1,3,5,6,F	file.Length=MaxFileSize+1, file.FileName="cover.jpg", file.ContentType="image/jpeg"	Throws ArgumentException: "La imagen de portada no puede superar los 5 MB."
4	I,1,3,5,7,8,9,F	file.Length=1024, file.FileName="cover.gif", file.ContentType="image/gif"	Throws ArgumentException: "El formato de la portada debe ser JPG, JPEG, PNG o WEBP."
5	I,1,3,5,7,8,10,11,12,F	file.Length=1024, file.FileName="cover.jpg", file.ContentType="application/pdf"	Throws ArgumentException: "El tipo de archivo de la portada no es válido."
6	I,1,3,5,7,8,10,F	file.Length=1024, file.FileName="cover.jpg", file.ContentType=" "	Returns normally. The content type is whitespace, so validation is skipped.
7	I,1,3,5,7,8,10,11,F	file.Length=1024, file.FileName="cover.jpg", file.ContentType="image/jpeg"	Returns normally. The extension and content type are allowed.
# GetByIdAsync() 

@startuml
title Grafo Function GetByIdAsync

skinparam usecase {
  BackgroundColor White
  BorderColor Black
  BorderThickness 2
  FontColor #32c9a6
  FontStyle bold
  FontSize 18
}
skinparam ArrowColor Black
skinparam shadowing false

usecase "I" as NI
usecase "1" as N1
usecase "2" as N2
usecase "3" as N3
usecase "4" as N4
usecase "5" as N5
usecase "F" as NF

NI --> N1
N1 --> N2
N2 --> N4 : True
N2 --> N3 : False
N3 --> N4 : True
N3 --> N5 : False
N4 --> NF
N5 --> NF

legend right
| Nodo | Código |
| I | Initialize parameter id |
| 1 | GetByIdAsync(id) |
| 2 | book == null |
| 3 | book.IsActive != true |
| 4 | Return null |
| 5 | MapToDtoAsync(book) and return |
| F | End |
endlegend

@enduml


Nro	Camino	Entrada	Salida
1	I,1,2,4,F	id=1, repository returns null	null. The book does not exist, so the second condition is skipped.
2	I,1,2,3,4,F	id=1, repository returns a book with IsActive=false	null. The book exists but is inactive.
3	I,1,2,3,5,F	id=1, repository returns a book with IsActive=true, and MapToDtoAsync returns a valid DTO	The mapped BookDto is returned.


# DeleteAsync(uint id)

@startuml
title Grafo Function DeleteAsync

skinparam usecase {
  BackgroundColor White
  BorderColor Black
  BorderThickness 2
  FontColor #32c9a6
  FontStyle bold
  FontSize 18
}
skinparam ArrowColor Black
skinparam shadowing false

usecase "I" as NI
usecase "1" as N1
usecase "2" as N2
usecase "3" as N3
usecase "4" as N4
usecase "5" as N5
usecase "F" as NF

NI --> N1
N1 --> N2
N2 --> N4 : True
N2 --> N3 : False
N3 --> N4 : True
N3 --> N5 : False
N4 --> NF
N5 --> NF

legend right
| Nodo | Código |
| I | Initialize parameter id |
| 1 | GetByIdAsync(id) |
| 2 | book == null |
| 3 | book.IsActive != true |
| 4 | Return false |
| 5 | DeleteAsync(book) |
| F | Return true / end |
endlegend

@enduml

Nro	Camino	Entrada	Salida
1	I,1,2,4,F	id=1, repository returns null	false. The book does not exist, so deletion is not performed.
2	I,1,2,3,4,F	id=1, repository returns a book with IsActive=false	false. The book is inactive, so deletion is not performed.
3	I,1,2,3,5,F	id=1, repository returns a book with IsActive=true, and DeleteAsync(book) completes successfully	true. The repository deletion method is called and the function returns true.

# ValidateAuthorsAsync()

@startuml
title Grafo Function ValidateAuthorsAsync

skinparam usecase {
  BackgroundColor White
  BorderColor Black
  BorderThickness 2
  FontColor #32c9a6
  FontStyle bold
  FontSize 18
}
skinparam ArrowColor Black
skinparam shadowing false

usecase "I" as NI
usecase "1" as N1
usecase "2" as N2
usecase "3" as N3
usecase "4" as N4
usecase "5" as N5
usecase "6" as N6
usecase "F" as NF

NI --> N1
N1 --> N2
N2 --> N3
N3 --> N4 : True
N3 --> NF : False
N4 --> N5
N5 --> N6 : True
N5 --> N3 : False
N6 --> NF : Exception

legend right
| Nodo | Código |
| I | Initialize authorIds |
| 1 | Convert authorIds to list |
| 2 | ValidateAuthorIds(ids) |
| 3 | foreach: another author ID exists |
| 4 | ExistsActiveByIdAsync(authorId) |
| 5 | !exists |
| 6 | Throw InvalidOperationException |
| F | Normal completion / exception exit |
endlegend

@enduml

Nro	Camino	Entrada	Salida
1	I,1,2,3,F	authorIds=[], ValidateAuthorIds accepts the empty list	Completes normally. The loop executes zero times, and the repository is never called.
2	I,1,2,3,4,5,3,F	authorIds=[10], ValidateAuthorIds accepts the list, ExistsActiveByIdAsync(10) returns true	Completes normally after confirming author 10 exists and is active.
3	I,1,2,3,4,5,6,F	authorIds=[10], ValidateAuthorIds accepts the list, ExistsActiveByIdAsync(10) returns false	Throws InvalidOperationException with message "El autor con Id 10 no existe o está inactivo."

# ValidateCategoriesAsync()
@startuml
title Grafo Function ValidateCategoriesAsync

skinparam usecase {
  BackgroundColor White
  BorderColor Black
  BorderThickness 2
  FontColor #32c9a6
  FontStyle bold
  FontSize 18
}
skinparam ArrowColor Black
skinparam shadowing false

usecase "I" as NI
usecase "1" as N1
usecase "2" as N2
usecase "3" as N3
usecase "4" as N4
usecase "5" as N5
usecase "6" as N6
usecase "F" as NF

NI --> N1
N1 --> N2
N2 --> N3
N3 --> N4 : True
N3 --> NF : False
N4 --> N5
N5 --> N6 : True
N5 --> N3 : False
N6 --> NF : Exception

legend right
| Nodo | Código |
| I | Initialize categoryIds |
| 1 | Convert categoryIds to list |
| 2 | ValidateCategoryIds(ids) |
| 3 | foreach: another category ID exists |
| 4 | ExistsActiveByIdAsync(categoryId) |
| 5 | !exists |
| 6 | Throw InvalidOperationException |
| F | Normal completion / exception exit |
endlegend

@enduml

Nro	Camino	Entrada	Salida
1	I,1,2,3,F	categoryIds=[], ValidateCategoryIds accepts the empty list	Completes normally. The loop executes zero times, and the repository is never called.
2	I,1,2,3,4,5,3,F	categoryIds=[2], ValidateCategoryIds accepts the list, ExistsActiveByIdAsync(2) returns true	Completes normally after confirming category 2 exists and is active.
3	I,1,2,3,4,5,6,F	categoryIds=[2], ValidateCategoryIds accepts the list, ExistsActiveByIdAsync(2) returns false	Throws InvalidOperationException with message "La categoría con Id 2 no existe o está inactiva."

# ValidateIsbnAsync()
@startuml
title Grafo Function ValidateIsbnAsync

skinparam usecase {
  BackgroundColor White
  BorderColor Black
  BorderThickness 2
  FontColor #32c9a6
  FontStyle bold
  FontSize 18
}
skinparam ArrowColor Black
skinparam shadowing false

usecase "I" as NI
usecase "1" as N1
usecase "2" as N2
usecase "3" as N3
usecase "4" as N4
usecase "5" as N5
usecase "F" as NF

NI --> N1
N1 --> N2 : True
N1 --> N3 : False
N2 --> NF
N3 --> N4
N4 --> N5 : True
N4 --> NF : False
N5 --> NF : Exception

legend right
| Nodo | Código |
| I | Receive isbn |
| 1 | Is ISBN null, empty, or whitespace? |
| 2 | Return |
| 3 | Call ExistsByIsbnAsync(isbn) |
| 4 | Does ISBN already exist? |
| 5 | Throw InvalidOperationException |
| F | Function ends |
endlegend

@enduml

Nro	Camino	Entrada	Salida
1	I → 1 → 2 → F	isbn = null	Returns normally without calling ExistsByIsbnAsync.
2	I → 1 → 3 → 4 → F	isbn = "9781234567890"; repository returns false	Completes normally because the ISBN does not already exist.
3	I → 1 → 3 → 4 → 5 → F	isbn = "9781234567890"; repository returns true	Throws InvalidOperationException with message "Ya existe un libro activo con ese ISBN."
# ValidateUpdatesIsbnAsync()

@startuml
title Grafo Function ValidateUpdateIsbnAsync

skinparam usecase {
  BackgroundColor White
  BorderColor Black
  BorderThickness 2
  FontColor #32c9a6
  FontStyle bold
  FontSize 18
}
skinparam ArrowColor Black
skinparam shadowing false

usecase "I" as NI
usecase "1" as N1
usecase "2" as N2
usecase "3" as N3
usecase "4" as N4
usecase "5" as N5
usecase "6" as N6
usecase "F" as NF

NI --> N1
N1 --> N3 : True
N1 --> N2 : False
N2 --> N3 : True
N2 --> N4 : False
N3 --> NF
N4 --> N5
N5 --> N6 : True
N5 --> NF : False
N6 --> NF : Exception

legend right
| Nodo | Código |
| I | Receive newIsbn and currentIsbn |
| 1 | Is newIsbn null, empty, or whitespace? |
| 2 | Is newIsbn equal to currentIsbn? |
| 3 | Return |
| 4 | Call ExistsByIsbnAsync(newIsbn) |
| 5 | Does ISBN already exist? |
| 6 | Throw InvalidOperationException |
| F | Function ends |
endlegend

@enduml

Nro	Camino	Entrada	Salida
1	I, 1 (True), 3, F	newIsbn = null, currentIsbn = "123"	Returns without checking the repository.
2	I, 1 (False), 2 (True), 3, F	newIsbn = "123", currentIsbn = "123"	Returns because the ISBN has not changed.
3	I, 1 (False), 2 (False), 4, 5 (False), F	newIsbn = "456", currentIsbn = "123", repository returns false	Completes normally because the new ISBN is available.
4	I, 1 (False), 2 (False), 4, 5 (True), 6, F	newIsbn = "456", currentIsbn = "123", repository returns true	Throws InvalidOperationException with message "Ya existe un libro activo con ese ISBN."
# SaveCoverAsync()
@startuml
title Grafo Function SaveCoverAsync

skinparam usecase {
  BackgroundColor White
  BorderColor Black
  BorderThickness 2
  FontColor #32c9a6
  FontStyle bold
  FontSize 18
}
skinparam ArrowColor Black
skinparam shadowing false

usecase "I" as NI
usecase "1" as N1
usecase "2" as N2
usecase "3" as N3
usecase "4" as N4
usecase "5" as N5
usecase "6" as N6
usecase "7" as N7
usecase "8" as N8
usecase "9" as N9
usecase "10" as N10
usecase "11" as N11
usecase "12" as N12
usecase "F" as NF

NI --> N1
N1 --> N3 : True
N1 --> N2 : False
N2 --> N3 : True
N2 --> N4 : False
N3 --> NF
N4 --> N5
N5 --> N6
N6 --> N7
N7 --> N8
N8 --> N9
N9 --> N10
N10 --> N11
N11 --> N12
N12 --> NF

legend right
| Nodo | Código |
| I | Receive file |
| 1 | file == null |
| 2 | file.Length <= 0 |
| 3 | Return null |
| 4 | GetWebRootPath() |
| 5 | Build uploads path |
| 6 | CreateDirectory(uploadsPath) |
| 7 | Get and normalize extension |
| 8 | Generate unique file name |
| 9 | Build file path |
| 10 | Create FileStream |
| 11 | CopyToAsync(stream) |
| 12 | Return file URL |
| F | Function ends |
endlegend

@enduml

Nro	Camino	Entrada	Salida
1	I, 1 (True), 3, F	file = null	Returns null.
2	I, 1 (False), 2 (True), 3, F	Non-null file with Length = 0	Returns null.
3	I, 1 (False), 2 (False), 4, 5, 6, 7, 8, 9, 10, 11, 12, F	Valid non-empty file, e.g. cover.JPG	Saves the file with a unique name and returns /uploads/books/{fileName} with the extension normalized to .jpg.


# DeleteCoverImage()
@startuml
title Grafo Function DeleteCoverImage

skinparam usecase {
  BackgroundColor White
  BorderColor Black
  BorderThickness 2
  FontColor #32c9a6
  FontStyle bold
  FontSize 18
}
skinparam ArrowColor Black
skinparam shadowing false

usecase "I" as NI
usecase "1" as N1
usecase "2" as N2
usecase "3" as N3
usecase "4" as N4
usecase "5" as N5
usecase "6" as N6
usecase "7" as N7
usecase "8" as N8
usecase "9" as N9
usecase "10" as N10
usecase "F" as NF

NI --> N1
N1 --> N2 : True
N1 --> N3 : False
N2 --> NF
N3 --> N4 : True
N3 --> N5 : False
N4 --> NF
N5 --> N6
N6 --> N7 : True
N6 --> N8 : False
N7 --> NF
N8 --> N9
N9 --> N10 : True
N9 --> NF : False
N10 --> NF

legend right
| Nodo | Código |
| I | Receive coverImage and exceptPath |
| 1 | Is coverImage null, empty, or whitespace? |
| 2 | Return |
| 3 | Does coverImage equal exceptPath, ignoring case? |
| 4 | Return |
| 5 | Extract file name |
| 6 | Is fileName null, empty, or whitespace? |
| 7 | Return |
| 8 | Build file path |
| 9 | Does the file exist? |
| 10 | Delete file |
| F | Function ends |
endlegend

@enduml


Nro	Camino	Entrada	Salida
1	I, 1 (True), 2, F	coverImage = null	Returns without deleting a file.
2	I, 1 (False), 3 (True), 4, F	coverImage = "/uploads/books/cover.jpg", exceptPath = "/UPLOADS/BOOKS/COVER.JPG"	Returns without deleting the file because the paths match ignoring case.
3	I, 1 (False), 3 (False), 5, 6 (True), 7, F	coverImage = "/", exceptPath = null	Returns without deleting a file because the extracted file name is empty.
4	I, 1 (False), 3 (False), 5, 6 (False), 8, 9 (False), F	coverImage = "/uploads/books/cover.jpg", exceptPath = null, file does not exist	Completes without deleting anything.
5	I, 1 (False), 3 (False), 5, 6 (False), 8, 9 (True), 10, F	coverImage = "/uploads/books/cover.jpg", exceptPath = null, file exists	Deletes the file.


# SearchAsync()

@startuml
title Grafo Function SearchAsync

skinparam usecase {
  BackgroundColor White
  BorderColor Black
  BorderThickness 2
  FontColor #32c9a6
  FontStyle bold
  FontSize 18
}
skinparam ArrowColor Black
skinparam shadowing false

usecase "I" as NI
usecase "1" as N1
usecase "2" as N2
usecase "3" as N3
usecase "4" as N4
usecase "F" as NF

NI --> N1
N1 --> N2
N2 --> N3
N3 --> N4 : True
N4 --> N3
N3 --> NF : False

legend right
| Nodo | Código |
| I | Receive phrase |
| 1 | SearchAsync(phrase) |
| 2 | Initialize result list |
| 3 | Another book exists? |
| 4 | Map book and add to result |
| F | Return result |
endlegend

@enduml

Nro	Camino	Entrada	Salida
1	I, 1, 2, 3 (False), F	Repository returns an empty collection	Returns an empty IEnumerable<BookDto>.
2	I, 1, 2, 3 (True), 4, 3 (False), F	Repository returns one book	Maps the book to a BookDto and returns a collection containing one DTO.
# GetAllAsync()
@startuml
title Grafo Function GetAllAsync

skinparam usecase {
  BackgroundColor White
  BorderColor Black
  BorderThickness 2
  FontColor #32c9a6
  FontStyle bold
  FontSize 18
}
skinparam ArrowColor Black
skinparam shadowing false

usecase "I" as NI
usecase "1" as N1
usecase "2" as N2
usecase "F" as NF

NI --> N1
N1 --> N2
N2 --> NF

legend right
| Nodo | Código |
| I | Start function |
| 1 | GetAllAsync() |
| 2 | MapWithAvailabilityAsync(books) |
| F | Return result |
endlegend

@enduml

Nro	Camino	Entrada	Salida
1	I, 1, 2, F	Repository returns a collection of books	Returns the collection produced by MapWithAvailabilityAsync(books).

# GetAvailableAsync()

@startuml
title Grafo Function GetAvailableAsync

skinparam usecase {
  BackgroundColor White
  BorderColor Black
  BorderThickness 2
  FontColor #32c9a6
  FontStyle bold
  FontSize 18
}
skinparam ArrowColor Black
skinparam shadowing false

usecase "I" as NI
usecase "1" as N1
usecase "2" as N2
usecase "F" as NF

NI --> N1
N1 --> N2
N2 --> NF

legend right
| Nodo | Código |
| I | Start function |
| 1 | GetAvailableAsync() |
| 2 | MapWithAvailabilityAsync(books) |
| F | Return result |
endlegend

@enduml

Nro	Camino	Entrada	Salida
1	I, 1, 2, F	Repository returns a collection of available books	Returns the collection produced by MapWithAvailabilityAsync(books).