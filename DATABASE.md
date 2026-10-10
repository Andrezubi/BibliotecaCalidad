Use this sql as a reference, not as an executable

CREATE DATABASE `librarydb2`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE `librarydb2`;


-- ============================================================
-- 1. USER
-- ============================================================

CREATE TABLE `User` (
    `Id` SMALLINT UNSIGNED NOT NULL AUTO_INCREMENT,

    `CI` INT NOT NULL,
    `Complement` VARCHAR(2) NULL,

    `FirstName` VARCHAR(50) NOT NULL,
    `LastName` VARCHAR(50) NOT NULL,
    `SecondLastName` VARCHAR(50) NULL,

    `Phone` VARCHAR(15) NULL,

    `Username` VARCHAR(15) NOT NULL,
    `PasswordHash` VARCHAR(100) NOT NULL,

    `Role` VARCHAR(20) NOT NULL,

    `IsLibraryComputerUser` BOOLEAN NOT NULL DEFAULT FALSE,

    `Status` ENUM(
        'Active',
        'Blocked',
        'Inactive'
    ) NOT NULL DEFAULT 'Active',

    `IsActive` BOOLEAN NOT NULL DEFAULT TRUE,

    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL DEFAULT NULL,
    `UserId` SMALLINT UNSIGNED NULL,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_User_Username` (`Username`),

    UNIQUE KEY `UQ_User_CI_Complement`
        (`CI`, `Complement`),

    KEY `IX_User_UserId` (`UserId`),

    CONSTRAINT `FK_User_User`
        FOREIGN KEY (`UserId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL
)
ENGINE = InnoDB;


-- ============================================================
-- 2. AUTHOR
-- ============================================================

CREATE TABLE `Author` (
    `Id` INT UNSIGNED NOT NULL AUTO_INCREMENT,

    `FirstName` VARCHAR(50) NOT NULL,
    `LastName` VARCHAR(50) NOT NULL,
    `SecondLastName` VARCHAR(50) NULL,

    `IsActive` BOOLEAN NOT NULL DEFAULT TRUE,

    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL DEFAULT NULL,
    `UserId` SMALLINT UNSIGNED NULL,

    PRIMARY KEY (`Id`),

    KEY `IX_Author_UserId` (`UserId`),

    CONSTRAINT `FK_Author_User`
        FOREIGN KEY (`UserId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL
)
ENGINE = InnoDB;


-- ============================================================
-- 3. PUBLISHER
-- ============================================================

CREATE TABLE `Publisher` (
    `Id` INT UNSIGNED NOT NULL AUTO_INCREMENT,

    `Name` VARCHAR(150) NOT NULL,

    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL DEFAULT NULL,
    `UserId` SMALLINT UNSIGNED NULL,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_Publisher_Name` (`Name`),

    KEY `IX_Publisher_UserId` (`UserId`),

    CONSTRAINT `FK_Publisher_User`
        FOREIGN KEY (`UserId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL
)
ENGINE = InnoDB;


-- ============================================================
-- 4. BOOK
-- ============================================================

CREATE TABLE `Book` (
    `Id` INT UNSIGNED NOT NULL AUTO_INCREMENT,

    `Title` VARCHAR(255) NOT NULL,

    `EditionNumber` TINYINT UNSIGNED NOT NULL,

    `ISBN` VARCHAR(20) NULL,

    `PublicationYear` SMALLINT UNSIGNED NULL,

    `PublisherId` INT UNSIGNED NULL,

    `PageCount` SMALLINT UNSIGNED NULL,

    `Description` VARCHAR(400) NULL,

    `CoverImage` VARCHAR(500) NULL,

    `IsActive` BOOLEAN NOT NULL DEFAULT TRUE,

    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL DEFAULT NULL,
    `UserId` SMALLINT UNSIGNED NULL,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_Book_ISBN` (`ISBN`),

    KEY `IX_Book_Title` (`Title`),
    KEY `IX_Book_EditionNumber` (`EditionNumber`),
    KEY `IX_Book_PublisherId` (`PublisherId`),
    KEY `IX_Book_UserId` (`UserId`),

    CONSTRAINT `FK_Book_Publisher`
        FOREIGN KEY (`PublisherId`)
        REFERENCES `Publisher` (`Id`)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT `FK_Book_User`
        FOREIGN KEY (`UserId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL
)
ENGINE = InnoDB;


-- ============================================================
-- 5. CATEGORY
-- ============================================================

CREATE TABLE `Category` (
    `Id` TINYINT UNSIGNED NOT NULL AUTO_INCREMENT,

    `Name` VARCHAR(50) NOT NULL,
    `Description` VARCHAR(500) NULL,

    `IsActive` BOOLEAN NOT NULL DEFAULT TRUE,

    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL DEFAULT NULL,
    `UserId` SMALLINT UNSIGNED NULL,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_Category_Name` (`Name`),

    KEY `IX_Category_UserId` (`UserId`),

    CONSTRAINT `FK_Category_User`
        FOREIGN KEY (`UserId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL
)
ENGINE = InnoDB;


-- ============================================================
-- 6. BOOK AUTHOR
-- ============================================================

CREATE TABLE `BookAuthor` (
    `BookId` INT UNSIGNED NOT NULL,
    `AuthorId` INT UNSIGNED NOT NULL,

    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL DEFAULT NULL,
    `UserId` SMALLINT UNSIGNED NULL,

    PRIMARY KEY (`BookId`, `AuthorId`),

    KEY `IX_BookAuthor_AuthorId` (`AuthorId`),
    KEY `IX_BookAuthor_UserId` (`UserId`),

    CONSTRAINT `FK_BookAuthor_Book`
        FOREIGN KEY (`BookId`)
        REFERENCES `Book` (`Id`)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT `FK_BookAuthor_Author`
        FOREIGN KEY (`AuthorId`)
        REFERENCES `Author` (`Id`)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT `FK_BookAuthor_User`
        FOREIGN KEY (`UserId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL
)
ENGINE = InnoDB;


-- ============================================================
-- 7. BOOK CATEGORY
-- ============================================================

CREATE TABLE `BookCategory` (
    `BookId` INT UNSIGNED NOT NULL,
    `CategoryId` TINYINT UNSIGNED NOT NULL,

    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL DEFAULT NULL,
    `UserId` SMALLINT UNSIGNED NULL,

    PRIMARY KEY (`BookId`, `CategoryId`),

    KEY `IX_BookCategory_CategoryId` (`CategoryId`),
    KEY `IX_BookCategory_UserId` (`UserId`),

    CONSTRAINT `FK_BookCategory_Book`
        FOREIGN KEY (`BookId`)
        REFERENCES `Book` (`Id`)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT `FK_BookCategory_Category`
        FOREIGN KEY (`CategoryId`)
        REFERENCES `Category` (`Id`)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT `FK_BookCategory_User`
        FOREIGN KEY (`UserId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL
)
ENGINE = InnoDB;


-- ============================================================
-- 8. COPY STATUS
-- ============================================================

CREATE TABLE `CopyStatus` (
    `Id` TINYINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `Name` VARCHAR(30) NOT NULL,

    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL DEFAULT NULL,
    `UserId` SMALLINT UNSIGNED NULL,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_CopyStatus_Name` (`Name`),

    KEY `IX_CopyStatus_UserId` (`UserId`),

    CONSTRAINT `FK_CopyStatus_User`
        FOREIGN KEY (`UserId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL
)
ENGINE = InnoDB;


-- ============================================================
-- 9. COPY
-- ============================================================

CREATE TABLE `Copy` (
    `Id` INT UNSIGNED NOT NULL AUTO_INCREMENT,

    `BookId` INT UNSIGNED NOT NULL,

    `InternalCode` VARCHAR(50) NOT NULL,

    `StatusId` TINYINT UNSIGNED NOT NULL DEFAULT 1,

    `IsActive` BOOLEAN NOT NULL DEFAULT TRUE,

    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL DEFAULT NULL,
    `UserId` SMALLINT UNSIGNED NULL,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_Copy_InternalCode` (`InternalCode`),

    KEY `IX_Copy_BookId` (`BookId`),
    KEY `IX_Copy_StatusId` (`StatusId`),
    KEY `IX_Copy_UserId` (`UserId`),

    CONSTRAINT `FK_Copy_Book`
        FOREIGN KEY (`BookId`)
        REFERENCES `Book` (`Id`)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT `FK_Copy_Status`
        FOREIGN KEY (`StatusId`)
        REFERENCES `CopyStatus` (`Id`)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT `FK_Copy_User`
        FOREIGN KEY (`UserId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL
)
ENGINE = InnoDB;


-- ============================================================
-- 10. LOAN
-- ============================================================

CREATE TABLE `Loan` (
    `Id` INT UNSIGNED NOT NULL AUTO_INCREMENT,

    `BorrowerId` SMALLINT UNSIGNED NOT NULL,
    `LibrarianId` SMALLINT UNSIGNED NOT NULL,

    `LoanDate` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `DueDate` DATETIME NOT NULL,

    `Status` ENUM(
        'Active',
        'Completed',
        'Overdue',
        'Lost'
    ) NOT NULL DEFAULT 'Active',

    `IsActive` BOOLEAN NOT NULL DEFAULT TRUE,

    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL DEFAULT NULL,
    `UserId` SMALLINT UNSIGNED NULL,

    PRIMARY KEY (`Id`),

    KEY `IX_Loan_BorrowerId` (`BorrowerId`),
    KEY `IX_Loan_LibrarianId` (`LibrarianId`),
    KEY `IX_Loan_DueDate` (`DueDate`),
    KEY `IX_Loan_Status` (`Status`),
    KEY `IX_Loan_UserId` (`UserId`),

    CONSTRAINT `FK_Loan_Borrower`
        FOREIGN KEY (`BorrowerId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT `FK_Loan_Librarian`
        FOREIGN KEY (`LibrarianId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT `FK_Loan_User`
        FOREIGN KEY (`UserId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL
)
ENGINE = InnoDB;


-- ============================================================
-- 11. LOAN ITEM
-- ============================================================

CREATE TABLE `LoanItem` (
    `LoanId` INT UNSIGNED NOT NULL,
    `CopyId` INT UNSIGNED NOT NULL,

    `ReturnedAt` DATETIME NULL,

    `Status` ENUM(
        'Active',
        'Returned',
        'Overdue',
        'Lost'
    ) NOT NULL DEFAULT 'Active',

    `IsActive` BOOLEAN NOT NULL DEFAULT TRUE,

    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL DEFAULT NULL,
    `UserId` SMALLINT UNSIGNED NULL,

    PRIMARY KEY (`LoanId`, `CopyId`),

    KEY `IX_LoanItem_CopyId` (`CopyId`),
    KEY `IX_LoanItem_Status` (`Status`),
    KEY `IX_LoanItem_UserId` (`UserId`),

    CONSTRAINT `FK_LoanItem_Loan`
        FOREIGN KEY (`LoanId`)
        REFERENCES `Loan` (`Id`)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT `FK_LoanItem_Copy`
        FOREIGN KEY (`CopyId`)
        REFERENCES `Copy` (`Id`)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT `FK_LoanItem_User`
        FOREIGN KEY (`UserId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL
)
ENGINE = InnoDB;


-- ============================================================
-- 12. RENEWAL
-- ============================================================

CREATE TABLE `Renewal` (
    `Id` INT UNSIGNED NOT NULL AUTO_INCREMENT,

    `LoanId` INT UNSIGNED NOT NULL,
    `CopyId` INT UNSIGNED NOT NULL,

    `LibrarianId` SMALLINT UNSIGNED NOT NULL,

    `RenewalDate` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    `PreviousDueDate` DATETIME NOT NULL,
    `NewDueDate` DATETIME NOT NULL,

    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL DEFAULT NULL,
    `UserId` SMALLINT UNSIGNED NULL,

    PRIMARY KEY (`Id`),

    KEY `IX_Renewal_Loan_Copy`
        (`LoanId`, `CopyId`),

    KEY `IX_Renewal_LibrarianId`
        (`LibrarianId`),

    KEY `IX_Renewal_UserId`
        (`UserId`),

    CONSTRAINT `FK_Renewal_LoanItem`
        FOREIGN KEY (`LoanId`, `CopyId`)
        REFERENCES `LoanItem` (`LoanId`, `CopyId`)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT `FK_Renewal_Librarian`
        FOREIGN KEY (`LibrarianId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT `FK_Renewal_User`
        FOREIGN KEY (`UserId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL
)
ENGINE = InnoDB;


-- ============================================================
-- 13. FINE
-- ============================================================

CREATE TABLE `Fine` (
    `Id` INT UNSIGNED NOT NULL AUTO_INCREMENT,

    `LoanId` INT UNSIGNED NOT NULL,
    `CopyId` INT UNSIGNED NOT NULL,

    `DailyRate` DECIMAL(5,2) NOT NULL DEFAULT 10.00,

    `OverdueDays` INT UNSIGNED NOT NULL DEFAULT 0,

    `Amount` DECIMAL(7,2) NOT NULL DEFAULT 0.00,

    `Status` ENUM(
        'Pending',
        'PartiallyPaid',
        'Paid',
        'Waived'
    ) NOT NULL DEFAULT 'Pending',

    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL DEFAULT NULL,
    `UserId` SMALLINT UNSIGNED NULL,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_Fine_Loan_Copy`
        (`LoanId`, `CopyId`),

    KEY `IX_Fine_Status` (`Status`),
    KEY `IX_Fine_UserId` (`UserId`),

    CONSTRAINT `FK_Fine_LoanItem`
        FOREIGN KEY (`LoanId`, `CopyId`)
        REFERENCES `LoanItem` (`LoanId`, `CopyId`)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT `FK_Fine_User`
        FOREIGN KEY (`UserId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL
)
ENGINE = InnoDB;


-- ============================================================
-- 14. FINE PAYMENT
-- ============================================================

CREATE TABLE `FinePayment` (
    `Id` INT UNSIGNED NOT NULL AUTO_INCREMENT,

    `FineId` INT UNSIGNED NOT NULL,
    `LibrarianId` SMALLINT UNSIGNED NOT NULL,

    `Amount` DECIMAL(7,2) NOT NULL,

    `ReceiptNumber` VARCHAR(50) NOT NULL,

    `PaidAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL DEFAULT NULL,
    `UserId` SMALLINT UNSIGNED NULL,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_FinePayment_ReceiptNumber`
        (`ReceiptNumber`),

    KEY `IX_FinePayment_FineId` (`FineId`),
    KEY `IX_FinePayment_LibrarianId` (`LibrarianId`),
    KEY `IX_FinePayment_PaidAt` (`PaidAt`),
    KEY `IX_FinePayment_UserId` (`UserId`),

    CONSTRAINT `FK_FinePayment_Fine`
        FOREIGN KEY (`FineId`)
        REFERENCES `Fine` (`Id`)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT `FK_FinePayment_Librarian`
        FOREIGN KEY (`LibrarianId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT `FK_FinePayment_User`
        FOREIGN KEY (`UserId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL
)
ENGINE = InnoDB;


-- ============================================================
-- 15. RESERVATION
-- ============================================================

CREATE TABLE `Reservation` (
    `Id` INT UNSIGNED NOT NULL AUTO_INCREMENT,

    `ReaderId` SMALLINT UNSIGNED NOT NULL,
    `CopyId` INT UNSIGNED NOT NULL,

    `StartAt` DATETIME NOT NULL,
    `EndAt` DATETIME NOT NULL,

    `Status` ENUM(
        'Pending',
        'Active',
        'Completed',
        'Cancelled',
        'Expired'
    ) NOT NULL DEFAULT 'Pending',

    `IsActive` BOOLEAN NOT NULL DEFAULT TRUE,

    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL DEFAULT NULL,
    `UserId` SMALLINT UNSIGNED NULL,

    PRIMARY KEY (`Id`),

    KEY `IX_Reservation_ReaderId` (`ReaderId`),
    KEY `IX_Reservation_CopyId` (`CopyId`),
    KEY `IX_Reservation_StartAt` (`StartAt`),
    KEY `IX_Reservation_EndAt` (`EndAt`),
    KEY `IX_Reservation_Status` (`Status`),
    KEY `IX_Reservation_UserId` (`UserId`),

    CONSTRAINT `FK_Reservation_Reader`
        FOREIGN KEY (`ReaderId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT `FK_Reservation_Copy`
        FOREIGN KEY (`CopyId`)
        REFERENCES `Copy` (`Id`)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT `FK_Reservation_User`
        FOREIGN KEY (`UserId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL,

    CONSTRAINT `CHK_Reservation_Dates`
        CHECK (`EndAt` > `StartAt`)
)
ENGINE = InnoDB;


-- ============================================================
-- 16. LOAN REQUEST
-- ============================================================

CREATE TABLE `LoanRequest` (
    `Id` INT UNSIGNED NOT NULL AUTO_INCREMENT,

    `ReaderId` SMALLINT UNSIGNED NOT NULL,

    `RequestedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    `Status` ENUM(
        'Pending',
        'Approved',
        'Rejected',
        'Completed',
        'Cancelled'
    ) NOT NULL DEFAULT 'Pending',

    `ProcessedBy` SMALLINT UNSIGNED NULL,
    `ProcessedAt` DATETIME NULL,

    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL DEFAULT NULL,
    `UserId` SMALLINT UNSIGNED NULL,

    PRIMARY KEY (`Id`),

    KEY `IX_LoanRequest_ReaderId` (`ReaderId`),
    KEY `IX_LoanRequest_Status` (`Status`),
    KEY `IX_LoanRequest_RequestedAt` (`RequestedAt`),
    KEY `IX_LoanRequest_ProcessedBy` (`ProcessedBy`),
    KEY `IX_LoanRequest_UserId` (`UserId`),

    CONSTRAINT `FK_LoanRequest_Reader`
        FOREIGN KEY (`ReaderId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT `FK_LoanRequest_ProcessedBy`
        FOREIGN KEY (`ProcessedBy`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL,

    CONSTRAINT `FK_LoanRequest_User`
        FOREIGN KEY (`UserId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL
)
ENGINE = InnoDB;


-- ============================================================
-- 17. LOAN REQUEST ITEM
-- ============================================================

CREATE TABLE `LoanRequestItem` (
    `LoanRequestId` INT UNSIGNED NOT NULL,
    `CopyId` INT UNSIGNED NOT NULL,

    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL DEFAULT NULL,
    `UserId` SMALLINT UNSIGNED NULL,

    PRIMARY KEY (`LoanRequestId`, `CopyId`),

    KEY `IX_LoanRequestItem_CopyId` (`CopyId`),
    KEY `IX_LoanRequestItem_UserId` (`UserId`),

    CONSTRAINT `FK_LoanRequestItem_Request`
        FOREIGN KEY (`LoanRequestId`)
        REFERENCES `LoanRequest` (`Id`)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT `FK_LoanRequestItem_Copy`
        FOREIGN KEY (`CopyId`)
        REFERENCES `Copy` (`Id`)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT `FK_LoanRequestItem_User`
        FOREIGN KEY (`UserId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL
)
ENGINE = InnoDB;


-- ============================================================
-- 18. NOTIFICATION
-- ============================================================

CREATE TABLE `Notification` (
    `Id` INT UNSIGNED NOT NULL AUTO_INCREMENT,

    `RecipientId` SMALLINT UNSIGNED NOT NULL,

    `Type` VARCHAR(50) NOT NULL,

    `Title` VARCHAR(150) NOT NULL,

    `Message` VARCHAR(500) NOT NULL,

    `IsRead` BOOLEAN NOT NULL DEFAULT FALSE,

    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL DEFAULT NULL,
    `ReadAt` DATETIME NULL DEFAULT NULL,

    `UserId` SMALLINT UNSIGNED NULL,

    PRIMARY KEY (`Id`),

    KEY `IX_Notification_RecipientId` (`RecipientId`),
    KEY `IX_Notification_IsRead` (`IsRead`),
    KEY `IX_Notification_CreatedAt` (`CreatedAt`),
    KEY `IX_Notification_UserId` (`UserId`),

    CONSTRAINT `FK_Notification_Recipient`
        FOREIGN KEY (`RecipientId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT `FK_Notification_User`
        FOREIGN KEY (`UserId`)
        REFERENCES `User` (`Id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL
)
ENGINE = InnoDB;


-- ============================================================
-- INITIAL DATA
-- ============================================================

-- ------------------------------------------------------------
-- Copy statuses
-- ------------------------------------------------------------

INSERT INTO `CopyStatus` (
    `Id`,
    `Name`
)
VALUES
    (1, 'Available'),
    (2, 'Loaned'),
    (3, 'Reserved'),
    (4, 'Damaged'),
    (5, 'Lost'),
    (6, 'Removed');


-- ============================================================
-- END OF DATABASE
-- ============================================================