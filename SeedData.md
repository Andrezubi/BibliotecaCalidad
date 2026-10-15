-- ============================================================
-- SAMPLE DATA FOR librarydb2
-- MySQL 8+
-- Run AFTER creating the schema in the supplied database script.
-- Uses explicit IDs so the relationships below are predictable.
-- All demo accounts use the supplied ASP.NET Identity password hash.
-- ============================================================

USE `librarydb2`;
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 1;

-- ------------------------------------------------------------
-- 1. USERS
-- Computer-account naming convention: LIBPC-01, LIBPC-02, LIBPC-03
-- Display names: Biblioteca - Computadora 01, etc.
-- Password for every account is the exact hash supplied by the user.
-- ------------------------------------------------------------
INSERT INTO `User`
(`Id`,`CI`,`Complement`,`FirstName`,`LastName`,`SecondLastName`,`Phone`,
 `Username`,`PasswordHash`,`Role`,`IsLibraryComputerUser`,`Status`,`IsActive`,`CreatedAt`,`UpdatedAt`,`UserId`)
VALUES
(1, 10000001, NULL, 'Administrador', 'Sistema', NULL, '70000001', 'admin',
 'AQAAAAIAAYagAAAAEJyjbHgfdc1gvuf7MhSKY2Kgdr9WdLycLe+br7wq0PwKqGgnCKyxBWfBihsnOcWrCA==',
 'Admin', FALSE, 'Active', TRUE, '2026-10-01 08:00:00', NULL, NULL),
(2, 10000002, NULL, 'María', 'Fernández', 'Rojas', '70000002', 'mfernandez',
 'AQAAAAIAAYagAAAAEJyjbHgfdc1gvuf7MhSKY2Kgdr9WdLycLe+br7wq0PwKqGgnCKyxBWfBihsnOcWrCA==',
 'Librarian', FALSE, 'Active', TRUE, '2026-10-01 08:05:00', NULL, 1),
(3, 10000003, NULL, 'Carlos', 'Mendoza', 'Vargas', '70000003', 'cmendoza',
 'AQAAAAIAAYagAAAAEJyjbHgfdc1gvuf7MhSKY2Kgdr9WdLycLe+br7wq0PwKqGgnCKyxBWfBihsnOcWrCA==',
 'Librarian', FALSE, 'Active', TRUE, '2026-10-01 08:10:00', NULL, 1),
(4, 10000004, NULL, 'Ana', 'Rojas', 'Salvatierra', '70000004', 'arojas',
 'AQAAAAIAAYagAAAAEJyjbHgfdc1gvuf7MhSKY2Kgdr9WdLycLe+br7wq0PwKqGgnCKyxBWfBihsnOcWrCA==',
 'User', FALSE, 'Active', TRUE, '2026-10-01 09:00:00', NULL, 1),
(5, 10000005, NULL, 'Diego', 'Gutiérrez', 'Paz', '70000005', 'dgutierrez',
 'AQAAAAIAAYagAAAAEJyjbHgfdc1gvuf7MhSKY2Kgdr9WdLycLe+br7wq0PwKqGgnCKyxBWfBihsnOcWrCA==',
 'User', FALSE, 'Active', TRUE, '2026-10-01 09:10:00', NULL, 1),
(6, 10000006, NULL, 'Lucía', 'Vargas', 'Céspedes', '70000006', 'lvargas',
 'AQAAAAIAAYagAAAAEJyjbHgfdc1gvuf7MhSKY2Kgdr9WdLycLe+br7wq0PwKqGgnCKyxBWfBihsnOcWrCA==',
 'User', FALSE, 'Active', TRUE, '2026-10-01 09:20:00', NULL, 1),
(7, 10000007, NULL, 'Mateo', 'Quiroga', 'López', '70000007', 'mquiroga',
 'AQAAAAIAAYagAAAAEJyjbHgfdc1gvuf7MhSKY2Kgdr9WdLycLe+br7wq0PwKqGgnCKyxBWfBihsnOcWrCA==',
 'User', FALSE, 'Active', TRUE, '2026-10-01 09:30:00', NULL, 1),
(8, 10000008, NULL, 'Sofía', 'Rivera', 'Molina', '70000008', 'srivera',
 'AQAAAAIAAYagAAAAEJyjbHgfdc1gvuf7MhSKY2Kgdr9WdLycLe+br7wq0PwKqGgnCKyxBWfBihsnOcWrCA==',
 'User', FALSE, 'Active', TRUE, '2026-10-01 09:40:00', NULL, 1),
(9, 10000009, NULL, 'Biblioteca', 'Computadora 01', NULL, NULL, 'LIBPC-01',
 'AQAAAAIAAYagAAAAEJyjbHgfdc1gvuf7MhSKY2Kgdr9WdLycLe+br7wq0PwKqGgnCKyxBWfBihsnOcWrCA==',
 'User', TRUE, 'Active', TRUE, '2026-10-02 08:00:00', NULL, 1),
(10, 10000010, NULL, 'Biblioteca', 'Computadora 02', NULL, NULL, 'LIBPC-02',
 'AQAAAAIAAYagAAAAEJyjbHgfdc1gvuf7MhSKY2Kgdr9WdLycLe+br7wq0PwKqGgnCKyxBWfBihsnOcWrCA==',
 'User', TRUE, 'Active', TRUE, '2026-10-02 08:01:00', NULL, 1),
(11, 10000011, NULL, 'Biblioteca', 'Computadora 03', NULL, NULL, 'LIBPC-03',
 'AQAAAAIAAYagAAAAEJyjbHgfdc1gvuf7MhSKY2Kgdr9WdLycLe+br7wq0PwKqGgnCKyxBWfBihsnOcWrCA==',
 'User', TRUE, 'Active', TRUE, '2026-10-02 08:02:00', NULL, 1),
(12, 10000012, NULL, 'Elena', 'Suárez', 'Torrez', '70000012', 'esuarez',
 'AQAAAAIAAYagAAAAEJyjbHgfdc1gvuf7MhSKY2Kgdr9WdLycLe+br7wq0PwKqGgnCKyxBWfBihsnOcWrCA==',
 'User', FALSE, 'Blocked', TRUE, '2026-10-02 10:00:00', NULL, 1);

-- ------------------------------------------------------------
-- 2. PUBLISHERS
-- ------------------------------------------------------------
INSERT INTO `Publisher` (`Id`,`Name`,`CreatedAt`,`UpdatedAt`,`UserId`) VALUES
(1,'Editorial Andina','2026-10-02 10:00:00',NULL,1),
(2,'Penguin Random House','2026-10-02 10:00:00',NULL,1),
(3,'Planeta','2026-10-02 10:00:00',NULL,1),
(4,'O''Reilly Media','2026-10-02 10:00:00',NULL,1),
(5,'Pearson','2026-10-02 10:00:00',NULL,1),
(6,'Editorial Juventud','2026-10-02 10:00:00',NULL,1),
(7,'Fondo de Cultura Económica','2026-10-02 10:00:00',NULL,1);

-- ------------------------------------------------------------
-- 3. BOOKS
-- ISBNs below are illustrative catalog data for development/testing.
-- ------------------------------------------------------------
INSERT INTO `Book`
(`Id`,`Title`,`EditionNumber`,`ISBN`,`PublicationYear`,`PublisherId`,`PageCount`,`Description`,`CoverImage`,`IsActive`,`CreatedAt`,`UpdatedAt`,`UserId`)
VALUES
(1,'Cien años de soledad',1,'9780307474728',1967,2,417,'Novela sobre varias generaciones de la familia Buendía en Macondo.',NULL,TRUE,'2026-10-02 10:15:00',NULL,1),
(2,'Don Quijote de la Mancha',1,'9788467033867',1605,3,1056,'Edición de una de las obras fundamentales de la literatura en español.',NULL,TRUE,'2026-10-02 10:15:00',NULL,1),
(3,'El principito',1,'9780156012195',1943,6,96,'Relato filosófico sobre la amistad, la imaginación y la responsabilidad.',NULL,TRUE,'2026-10-02 10:15:00',NULL,1),
(4,'1984',1,'9780451524935',1949,2,328,'Novela distópica sobre vigilancia, propaganda y poder.',NULL,TRUE,'2026-10-02 10:15:00',NULL,1),
(5,'Orgullo y prejuicio',1,'9780141439518',1813,2,432,'Novela sobre relaciones, reputación y clase social.',NULL,TRUE,'2026-10-02 10:15:00',NULL,1),
(6,'Clean Code',1,'9780132350884',2008,5,464,'Principios y prácticas para escribir código legible y mantenible.',NULL,TRUE,'2026-10-02 10:15:00',NULL,1),
(7,'The Pragmatic Programmer',2,'9780135957059',2019,5,352,'Consejos prácticos para el oficio del desarrollo de software.',NULL,TRUE,'2026-10-02 10:15:00',NULL,1),
(8,'Introduction to Algorithms',4,'9780262046305',2022,5,1312,'Texto académico sobre diseño y análisis de algoritmos.',NULL,TRUE,'2026-10-02 10:15:00',NULL,1),
(9,'Sapiens: De animales a dioses',1,'9788499924212',2011,3,496,'Una síntesis divulgativa de la historia de la humanidad.',NULL,TRUE,'2026-10-02 10:15:00',NULL,1),
(10,'Ficciones',1,'9780802130303',1944,7,174,'Colección de cuentos sobre laberintos, bibliotecas e ideas filosóficas.',NULL,TRUE,'2026-10-02 10:15:00',NULL,1),
(11,'Matilda',1,'9780142410370',1988,2,240,'Novela infantil sobre una niña con gran inteligencia y amor por los libros.',NULL,TRUE,'2026-10-02 10:15:00',NULL,1),
(12,'El mundo de Sofía',1,'9781858815309',1991,3,512,'Novela introductoria a la historia de la filosofía.',NULL,TRUE,'2026-10-02 10:15:00',NULL,1);

-- ------------------------------------------------------------
-- 4. CATEGORIES
-- ------------------------------------------------------------
INSERT INTO `Category` (`Id`,`Name`,`Description`,`IsActive`,`CreatedAt`,`UpdatedAt`,`UserId`) VALUES
(1,'Novela','Narrativa extensa de ficción.',TRUE,'2026-10-02 10:30:00',NULL,1),
(2,'Clásicos','Obras consideradas fundamentales en la literatura.',TRUE,'2026-10-02 10:30:00',NULL,1),
(3,'Ciencia ficción','Narrativa especulativa basada en ciencia o tecnología.',TRUE,'2026-10-02 10:30:00',NULL,1),
(4,'Programación','Libros de desarrollo de software y buenas prácticas.',TRUE,'2026-10-02 10:30:00',NULL,1),
(5,'Informática','Computación, algoritmos y sistemas.',TRUE,'2026-10-02 10:30:00',NULL,1),
(6,'Filosofía','Ideas filosóficas y pensamiento crítico.',TRUE,'2026-10-02 10:30:00',NULL,1),
(7,'Historia','Historia y evolución de las sociedades humanas.',TRUE,'2026-10-02 10:30:00',NULL,1),
(8,'Infantil y juvenil','Lecturas para público infantil y juvenil.',TRUE,'2026-10-02 10:30:00',NULL,1),
(9,'Cuento','Narraciones breves.',TRUE,'2026-10-02 10:30:00',NULL,1);

-- ------------------------------------------------------------
-- 5. AUTHORS
-- ------------------------------------------------------------
INSERT INTO `Author` (`Id`,`FirstName`,`LastName`,`SecondLastName`,`IsActive`,`CreatedAt`,`UpdatedAt`,`UserId`) VALUES
(1,'Gabriel','García','Márquez',TRUE,'2026-10-02 10:40:00',NULL,1),
(2,'Miguel','de Cervantes','Saavedra',TRUE,'2026-10-02 10:40:00',NULL,1),
(3,'Antoine','de Saint-Exupéry',NULL,TRUE,'2026-10-02 10:40:00',NULL,1),
(4,'George','Orwell',NULL,TRUE,'2026-10-02 10:40:00',NULL,1),
(5,'Jane','Austen',NULL,TRUE,'2026-10-02 10:40:00',NULL,1),
(6,'Robert','Martin','Cecil',TRUE,'2026-10-02 10:40:00',NULL,1),
(7,'David','Thomas',NULL,TRUE,'2026-10-02 10:40:00',NULL,1),
(8,'Andrew','Hunt',NULL,TRUE,'2026-10-02 10:40:00',NULL,1),
(9,'Thomas','Cormen','Leiserson',TRUE,'2026-10-02 10:40:00',NULL,1),
(10,'Charles','Rivest','Stein',TRUE,'2026-10-02 10:40:00',NULL,1),
(11,'Yuval Noah','Harari',NULL,TRUE,'2026-10-02 10:40:00',NULL,1),
(12,'Jorge Luis','Borges',NULL,TRUE,'2026-10-02 10:40:00',NULL,1),
(13,'Roald','Dahl',NULL,TRUE,'2026-10-02 10:40:00',NULL,1),
(14,'Jostein','Gaarder',NULL,TRUE,'2026-10-02 10:40:00',NULL,1);

-- ------------------------------------------------------------
-- 6. BOOK-AUTHOR RELATIONSHIPS
-- ------------------------------------------------------------
INSERT INTO `BookAuthor` (`BookId`,`AuthorId`,`CreatedAt`,`UpdatedAt`,`UserId`) VALUES
(1,1,'2026-10-02 11:00:00',NULL,1),
(2,2,'2026-10-02 11:00:00',NULL,1),
(3,3,'2026-10-02 11:00:00',NULL,1),
(4,4,'2026-10-02 11:00:00',NULL,1),
(5,5,'2026-10-02 11:00:00',NULL,1),
(6,6,'2026-10-02 11:00:00',NULL,1),
(7,7,'2026-10-02 11:00:00',NULL,1),
(7,8,'2026-10-02 11:00:00',NULL,1),
(8,9,'2026-10-02 11:00:00',NULL,1),
(8,10,'2026-10-02 11:00:00',NULL,1),
(9,11,'2026-10-02 11:00:00',NULL,1),
(10,12,'2026-10-02 11:00:00',NULL,1),
(11,13,'2026-10-02 11:00:00',NULL,1),
(12,14,'2026-10-02 11:00:00',NULL,1);

-- ------------------------------------------------------------
-- 7. BOOK-CATEGORY RELATIONSHIPS
-- ------------------------------------------------------------
INSERT INTO `BookCategory` (`BookId`,`CategoryId`,`CreatedAt`,`UpdatedAt`,`UserId`) VALUES
(1,1,'2026-10-02 11:10:00',NULL,1),(1,2,'2026-10-02 11:10:00',NULL,1),
(2,1,'2026-10-02 11:10:00',NULL,1),(2,2,'2026-10-02 11:10:00',NULL,1),
(3,8,'2026-10-02 11:10:00',NULL,1),(3,2,'2026-10-02 11:10:00',NULL,1),
(4,3,'2026-10-02 11:10:00',NULL,1),(4,2,'2026-10-02 11:10:00',NULL,1),
(5,1,'2026-10-02 11:10:00',NULL,1),(5,2,'2026-10-02 11:10:00',NULL,1),
(6,4,'2026-10-02 11:10:00',NULL,1),(6,5,'2026-10-02 11:10:00',NULL,1),
(7,4,'2026-10-02 11:10:00',NULL,1),(7,5,'2026-10-02 11:10:00',NULL,1),
(8,5,'2026-10-02 11:10:00',NULL,1),
(9,7,'2026-10-02 11:10:00',NULL,1),(9,6,'2026-10-02 11:10:00',NULL,1),
(10,9,'2026-10-02 11:10:00',NULL,1),(10,6,'2026-10-02 11:10:00',NULL,1),
(11,8,'2026-10-02 11:10:00',NULL,1),
(12,6,'2026-10-02 11:10:00',NULL,1),(12,1,'2026-10-02 11:10:00',NULL,1);

-- ------------------------------------------------------------
-- 8. COPY STATUS
-- Safe to run if the schema script already inserted these values.
-- ------------------------------------------------------------
INSERT INTO `CopyStatus` (`Id`,`Name`) VALUES
(1,'Available'),(2,'Loaned'),(3,'Reserved'),(4,'Damaged'),(5,'Lost'),(6,'Removed')
ON DUPLICATE KEY UPDATE `Name` = VALUES(`Name`);

-- ------------------------------------------------------------
-- 9. BOOK COPIES
-- ------------------------------------------------------------
INSERT INTO `Copy` (`Id`,`BookId`,`InternalCode`,`StatusId`,`IsActive`,`CreatedAt`,`UpdatedAt`,`UserId`) VALUES
(1,1,'LIB-COPY-0001',2,TRUE,'2026-10-02 11:30:00',NULL,1),
(2,1,'LIB-COPY-0002',2,TRUE,'2026-10-02 11:30:00',NULL,1),
(3,2,'LIB-COPY-0003',5,TRUE,'2026-10-02 11:30:00',NULL,1),
(4,2,'LIB-COPY-0004',2,TRUE,'2026-10-02 11:30:00',NULL,1),
(5,3,'LIB-COPY-0005',1,TRUE,'2026-10-02 11:30:00',NULL,1),
(6,3,'LIB-COPY-0006',2,TRUE,'2026-10-02 11:30:00',NULL,1),
(7,4,'LIB-COPY-0007',5,TRUE,'2026-10-02 11:30:00',NULL,1),
(8,4,'LIB-COPY-0008',5,TRUE,'2026-10-02 11:30:00',NULL,1),
(9,5,'LIB-COPY-0009',2,TRUE,'2026-10-02 11:30:00',NULL,1),
(10,6,'LIB-COPY-0010',2,TRUE,'2026-10-02 11:30:00',NULL,1),
(11,6,'LIB-COPY-0011',1,TRUE,'2026-10-02 11:30:00',NULL,1),
(12,7,'LIB-COPY-0012',1,TRUE,'2026-10-02 11:30:00',NULL,1),
(13,8,'LIB-COPY-0013',1,TRUE,'2026-10-02 11:30:00',NULL,1),
(14,9,'LIB-COPY-0014',2,TRUE,'2026-10-02 11:30:00',NULL,1),
(15,10,'LIB-COPY-0015',1,TRUE,'2026-10-02 11:30:00',NULL,1),
(16,11,'LIB-COPY-0016',1,TRUE,'2026-10-02 11:30:00',NULL,1),
(17,12,'LIB-COPY-0017',1,TRUE,'2026-10-02 11:30:00',NULL,1);

-- ------------------------------------------------------------
-- 10. LOANS
-- Fixed dates make Active/Overdue examples reproducible for demos.
-- Loan 1 active, Loan 2 completed, Loan 3 overdue, Loan 4 active,
-- Loan 5 lost. Status labels are exactly those defined in the schema.
-- ------------------------------------------------------------
INSERT INTO `Loan`
(`Id`,`BorrowerId`,`LibrarianId`,`LoanDate`,`DueDate`,`Status`,`IsActive`,`CreatedAt`,`UpdatedAt`,`UserId`)
VALUES
(1,4,2,'2026-10-10 10:00:00','2026-10-24 18:00:00','Active',TRUE,'2026-10-10 10:00:00',NULL,2),
(2,5,2,'2026-09-01 11:00:00','2026-09-15 18:00:00','Completed',TRUE,'2026-09-01 11:00:00','2026-09-12 15:30:00',2),
(3,6,3,'2026-09-20 14:00:00','2026-10-04 18:00:00','Overdue',TRUE,'2026-09-20 14:00:00',NULL,3),
(4,7,3,'2026-10-12 09:30:00','2026-10-26 18:00:00','Active',TRUE,'2026-10-12 09:30:00',NULL,3),
(5,8,2,'2026-08-10 12:00:00','2026-08-24 18:00:00','Lost',TRUE,'2026-08-10 12:00:00','2026-09-02 10:00:00',2);

-- ------------------------------------------------------------
-- 11. LOAN ITEMS
-- Each loan has multiple copies to demonstrate multi-item loans.
-- ------------------------------------------------------------
INSERT INTO `LoanItem`
(`LoanId`,`CopyId`,`ReturnedAt`,`Status`,`IsActive`,`CreatedAt`,`UpdatedAt`,`UserId`)
VALUES
(1,1,NULL,'Active',TRUE,'2026-10-10 10:00:00',NULL,2),
(1,10,NULL,'Active',TRUE,'2026-10-10 10:00:00',NULL,2),
(2,4,'2026-09-12 15:30:00','Returned',TRUE,'2026-09-01 11:00:00','2026-09-12 15:30:00',2),
(2,6,'2026-09-12 15:30:00','Returned',TRUE,'2026-09-01 11:00:00','2026-09-12 15:30:00',2),
(3,14,NULL,'Overdue',TRUE,'2026-09-20 14:00:00',NULL,3),
(3,2,NULL,'Overdue',TRUE,'2026-09-20 14:00:00',NULL,3),
(4,6,NULL,'Active',TRUE,'2026-10-12 09:30:00',NULL,3),
(4,9,NULL,'Active',TRUE,'2026-10-12 09:30:00',NULL,3),
(5,3,NULL,'Lost',TRUE,'2026-08-10 12:00:00','2026-09-02 10:00:00',2),
(5,8,NULL,'Lost',TRUE,'2026-08-10 12:00:00','2026-09-02 10:00:00',2);

-- ------------------------------------------------------------
-- OPTIONAL: quick verification queries
-- ------------------------------------------------------------
SELECT `Id`,`Username`,`Role`,`IsLibraryComputerUser`,`Status` FROM `User` ORDER BY `Id`;
SELECT `Id`,`Title`,`PublisherId` FROM `Book` ORDER BY `Id`;
SELECT `Id`,`BorrowerId`,`LibrarianId`,`LoanDate`,`DueDate`,`Status` FROM `Loan` ORDER BY `Id`;
SELECT `LoanId`,`CopyId`,`ReturnedAt`,`Status` FROM `LoanItem` ORDER BY `LoanId`,`CopyId`;
