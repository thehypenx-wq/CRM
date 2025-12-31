/* =========================================
   YEAR MASTER
========================================= */
CREATE TABLE YearMaster (
    YearId INT IDENTITY PRIMARY KEY,
    YearName NVARCHAR(MAX) NOT NULL,
    YearDisplay NVARCHAR(MAX),
    IsActive BIT DEFAULT 1,
    CreationDate DATETIME DEFAULT GETDATE()
);

/* =========================================
   COMPANY MASTER
========================================= */
CREATE TABLE CompanyMaster (
    CompId INT IDENTITY PRIMARY KEY,
    CompName NVARCHAR(MAX) NOT NULL,
    CompAddress1 NVARCHAR(MAX),
    CompAddress2 NVARCHAR(MAX),
    CompCity NVARCHAR(MAX),
    CompState NVARCHAR(MAX),
    CompCountry NVARCHAR(MAX),
    EmailId1 NVARCHAR(MAX),
    EmailId2 NVARCHAR(MAX),
    Phone NVARCHAR(MAX),
    MobileNo NVARCHAR(MAX),
    YearId INT,
    AuthUserId INT,
    IsActive BIT DEFAULT 1,
    GroupId INT,
    GroupName NVARCHAR(MAX),
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdateDate DATETIME,
    UpdateBy INT,
    Remark NVARCHAR(MAX)
);

/* =========================================
   SUPPLIER MASTER
========================================= */
CREATE TABLE SupplierMaster (
    SupplierId INT IDENTITY PRIMARY KEY,
    SupplierName NVARCHAR(MAX) NOT NULL,
    Address1 NVARCHAR(MAX),
    Address2 NVARCHAR(MAX),
    SupplierCity NVARCHAR(MAX),
    SupplierState NVARCHAR(MAX),
    SupplierCountry NVARCHAR(MAX),
    CompanyName NVARCHAR(MAX),
    GSTNo NVARCHAR(MAX),
    UniqueNo NVARCHAR(MAX),
    BankName NVARCHAR(MAX),
    BankBranch NVARCHAR(MAX),
    BankIFSC NVARCHAR(MAX),
    BankAccount NVARCHAR(MAX),
    Remark1 NVARCHAR(MAX),
    Remark2 NVARCHAR(MAX),
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    OpeningBalance DECIMAL(18,4) DEFAULT 0,
    AvailableBalance DECIMAL(18,4) DEFAULT 0,
    CreditBalance DECIMAL(18,4) DEFAULT 0,
    IsActive BIT DEFAULT 1,
    SupplierCode NVARCHAR(MAX),
    IsDelete BIT DEFAULT 0,
    DeletedBy INT,
    DeletedDate DATETIME,
    Remark NVARCHAR(MAX)
);

/* =========================================
   CATEGORY MASTER
========================================= */
CREATE TABLE CategoryMaster (
    CategoryId INT IDENTITY PRIMARY KEY,
    CategoryName NVARCHAR(MAX),
    CategoryImage NVARCHAR(MAX),
    CategoryCode NVARCHAR(MAX),
    Remark NVARCHAR(MAX),
    Desc1 NVARCHAR(MAX),
    Desc2 NVARCHAR(MAX),
    ShortDesc NVARCHAR(MAX),
    TitleName NVARCHAR(MAX),
    IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    IsDelete BIT DEFAULT 0,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);

/* =========================================
   SUB CATEGORY MASTER
========================================= */
CREATE TABLE SubCategoryMaster (
    SubCategoryId INT IDENTITY PRIMARY KEY,
    CategoryId INT,
    SubCategoryName NVARCHAR(MAX),
    SubCategoryCode NVARCHAR(MAX),
    HSNCode NVARCHAR(100),
    IsActive BIT DEFAULT 1,
    SubCategoryImage NVARCHAR(MAX),
    Remark NVARCHAR(MAX),
    Desc1 NVARCHAR(MAX),
    Desc2 NVARCHAR(MAX),
    ShortDesc NVARCHAR(MAX),
    TitleName NVARCHAR(MAX),
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    IsDelete BIT DEFAULT 0,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);

/* =========================================
   SIZE MASTER
========================================= */
CREATE TABLE SizeMaster (
    SizeId INT IDENTITY PRIMARY KEY,
    SizeName NVARCHAR(MAX),
    IsActive BIT DEFAULT 1,
    SizeType NVARCHAR(MAX),
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
);

/* =========================================
   COLOR MASTER
========================================= */
CREATE TABLE ColorMaster (
    ColorId INT IDENTITY PRIMARY KEY,
    ColorName NVARCHAR(MAX),
    IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
);


/* =========================================
   ATTRIBUTE MASTER
========================================= */
CREATE TABLE AttributeMaster (
    AttributeId INT IDENTITY PRIMARY KEY,
    AttributeName NVARCHAR(MAX),
    IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
);

/* =========================================
   ITEM MASTER
========================================= */
CREATE TABLE ItemMaster (
    ItemId INT IDENTITY PRIMARY KEY,
    ItemName NVARCHAR(MAX),
    CategoryId INT,
    SubCategoryId INT,
    ItemImage1 NVARCHAR(MAX),
    ItemImage2 NVARCHAR(MAX),
    ItemImage3 NVARCHAR(MAX),
    ItemMainImage NVARCHAR(MAX),
    ItemTitle NVARCHAR(MAX),
    ItemShortDesc NVARCHAR(MAX),
    ItemDesc NVARCHAR(MAX),
    ItemRemark NVARCHAR(MAX),
    Author NVARCHAR(MAX),
    Publisher NVARCHAR(MAX),
    IsActive BIT DEFAULT 1,
    IsPublish BIT DEFAULT 0,
    IsPublishECom BIT DEFAULT 0,
    PublishPer DECIMAL(18,4),
    ItemTag NVARCHAR(MAX),
    CGST DECIMAL(18,4),
    SGST DECIMAL(18,4),
    IGST DECIMAL(18,4),
    GST DECIMAL(18,4),
    Remark NVARCHAR(MAX),
    IsDelete BIT DEFAULT 0,
    MFGDate DATE,
    ExpiryDate DATE,
    ItemCompName NVARCHAR(MAX),
    ItemSKU NVARCHAR(Max),
    ItemBarcode NVARCHAR(MAX),
    ItemBarcodeImage NVARCHAR(MAX),
    Code NVARCHAR(MAX),
    Brand NVARCHAR(MAX),
    IsNegativeQTY BIT,
    HSNCode NVARCHAR(100),
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);

/* =========================================
   ITEM SIZE MAPPING
========================================= */
CREATE TABLE ItemSizeMaster (
    ISMId INT IDENTITY PRIMARY KEY,
    ItemId INT,
    SizeId INT,
    IsActive BIT,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
);

/* =========================================
   ITEM COLOR MAPPING
========================================= */
CREATE TABLE ItemColorMaster (
    ICMId INT IDENTITY PRIMARY KEY,
    ItemId INT,
    ColorId INT,
    IsActive BIT,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
);

/* =========================================
   ITEM ATTRIBUTE MAPPING
========================================= */
CREATE TABLE ItemAttributeMaster (
    IAMId INT IDENTITY PRIMARY KEY,
    ItemId INT,
    AttributeId INT,
    IsActive BIT,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
);


/* =========================================
   USERS
========================================= */
CREATE TABLE Users (
    UserId INT IDENTITY PRIMARY KEY,
    UserName NVARCHAR(MAX),
    UserImage NVARCHAR(MAX),
    FullName NVARCHAR(MAX),
    EmailAddress NVARCHAR(MAX),
    PhoneNo NVARCHAR(MAX),
    IsHypeNXAdmin BIT DEFAULT 0,
    IsAdmin BIT DEFAULT 0,
    UserPassword NVARCHAR(MAX),
    AuthUserId INT,
    IsActive BIT DEFAULT 1,
    ActivationDate DATETIME,
    CreationDate DATETIME DEFAULT GETDATE(),
    ExpiryDate DATETIME,
    IsDelete BIT DEFAULT 0,
    DeletedBy INT,
    DeletionDate DATETIME,
    UpdateDate DATETIME,
    UpdatedBy INT,
    Remark NVARCHAR(MAX),
    CreatedBy INT,
    CompId INT,
    YearId INT
);

/* =========================================
   PRIVILEGES
========================================= */
CREATE TABLE Privileges (
    PrivilegeId INT IDENTITY PRIMARY KEY,
    PrivilegeName NVARCHAR(MAX),
    CreationDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdateDate DATETIME,
    UpdatedBy INT,
    IsActive BIT DEFAULT 1
);

/* =========================================
   USER PRIVILEGES
========================================= */
CREATE TABLE UserPrivileges (
    UPId INT IDENTITY PRIMARY KEY,
    UserId INT,
    PrivilegeId INT,
    IsAccess BIT DEFAULT 0,
    CompanyId INT,
    YearId INT
);


/* =========================================
   Status Master
========================================= */
CREATE TABLE StatusMaster (
    StatusId INT IDENTITY PRIMARY KEY,
    StatusName NVARCHAR(MAX),
    StatusColor NVARCHAR(MAX),
    IsActive BIT DEFAULT 0,
    CompanyId INT,
    YearId INT,
    CreationDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdateDate DATETIME,
    UpdatedBy INT,
);


INSERT INTO StatusMaster
(
    StatusName,
    StatusColor,
    IsActive,
    CompanyId,
    YearId,
    CreatedBy
)
VALUES
-- Draft
('Draft', '#9E9E9E', 0, 1, 1, 1),

-- Processing
('Processing', '#2196F3', 0, 1, 1, 1),

-- On Hold
('OnHold', '#FFC107', 0, 1, 1, 1),

-- Payment Confirmed
('PaymentConfirmed', '#4CAF50', 0, 1, 1, 1),

-- Shipped
('Shipped', '#673AB7', 0, 1, 1, 1),

-- Out for Delivery
('OutforDelivery', '#FF9800', 0, 1, 1, 1),

-- Delivered
('Delivered', '#2E7D32', 0, 1, 1, 1),

-- Cancelled
('Cancelled', '#F44336', 0, 1, 1, 1),

-- Refunded
('Refunded', '#00BCD4', 0, 1, 1, 1),

-- Returned
('Returned', '#795548', 0, 1, 1, 1);



/* =========================================
   Payment Master
========================================= */
CREATE TABLE PaymentTypeMaster (
    PaymentTypeId INT IDENTITY PRIMARY KEY,
    PaymentTypeName NVARCHAR(MAX),
    IsActive BIT DEFAULT 0,
    CompanyId INT,
    YearId INT,
    CreationDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdateDate DATETIME,
    UpdatedBy INT,
);

INSERT INTO PaymentTypeMaster
(
    PaymentTypeName,
    IsActive,
    CompanyId,
    YearId,
    CreatedBy
)
VALUES
('Cash', 1, 1, 1, 1),
('Credit/Debit Cards', 1, 1, 1, 1),
('Cheque', 1, 1, 1, 1),
('UPI/Wallets', 1, 1, 1, 1),
('Gift Cards', 1, 1, 1, 1),
('Coupon Cards', 1, 1, 1, 1);



/* =========================================
   Purchase Master
========================================= */
CREATE TABLE PurchaseMaster (
    PurchaseId INT IDENTITY PRIMARY KEY,
    PurchaseInvoiceNo INT,
    PurchaseInvoiceNoPrefix NVARCHAR(100),
    PurchaseDate INT,
    SupplierId INT,
    SupplierInvoiceNo NVARCHAR(MAX),
    SupplierEntryDate INT,
    UniqueGUID NVARCHAR(MAX),
    GRNO NVARCHAR(100),
    TotalItem DECIMAL(18,4),
    TotalAmount DECIMAL(18,4),
    Remark NVARCHAR(MAX),
    IsSave BIT,
    IsActive BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    IsDelete BIT DEFAULT 0,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);


/* =========================================
   Purchase Transaction Master
========================================= */
CREATE TABLE PurchaseTransactionMaster (
    PurchaseTransactionId INT IDENTITY PRIMARY KEY,
    PurchaseMasterId INT,
    PurchaseInvoiceNo INT,
    PurchaseInvoiceNoPrefix NVARCHAR(100),
    PurchaseDate INT,
    SupplierId INT,
    CategoryId INT,
    SubCategoryId INT,
    ItemId INT,
    QTY DECIMAL(18,4),
    CostRate DECIMAL(18,4),
    CostPrice DECIMAL(18,4),
    SellRate DECIMAL(18,4),
    SellPrice DECIMAL(18,4),
    DiscountPer DECIMAL(18,4),
    DiscountAmount DECIMAL(18,4),
    MarkUpPer DECIMAL(18,4),
    MarkUpAmount DECIMAL(18,4),
    CGST DECIMAL(18,4),
    SGST DECIMAL(18,4),
    IGST DECIMAL(18,4),
    GST DECIMAL(18,4),
    IsIGST BIT DEFAULT 0,
    TaxPer DECIMAL(18,4),
    TaxAmount DECIMAL(18,4),
    ITaxPer DECIMAL(18,4),
    ITaxAmount DECIMAL(18,4),
    TotalAmount DECIMAL(18,4),
    ActualCostRate DECIMAL(18,4),
    Remark NVARCHAR(MAX),
    IsSave BIT,
    IsActive BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    IsDelete BIT DEFAULT 0,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);

/* =========================================
   Purchase Payment Master
========================================= */
CREATE TABLE PurchasePaymentMaster (
    PurchasePaymentId INT IDENTITY PRIMARY KEY,
    PurchaseMasterId INT,
    PurchaseInvoiceNo INT,
    PurchaseInvoiceNoPrefix NVARCHAR(100),
    PurchaseDate INT,
    SupplierId INT,
    PaymentTypeId INT,
    TotalAmount DECIMAL(18,4),
    Remark NVARCHAR(MAX),
    IsSave BIT,
    IsActive BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    IsDelete BIT DEFAULT 0,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);



/* =========================================
   Sales Master
========================================= */
CREATE TABLE SalesMaster (
    SalesId INT IDENTITY PRIMARY KEY,
    SalesInvoiceNo INT,
    SalesInvoiceNoPrefix NVARCHAR(100),
    SalesDate INT,
    CustomerId INT,
    UniqueGUID NVARCHAR(MAX),
    GRNO NVARCHAR(100),
    TotalItem DECIMAL(18,4),
    TotalAmount DECIMAL(18,4),
    TaxPer DECIMAL(18,4),
    TaxAmount DECIMAL(18,4),
    ShipingPer DECIMAL(18,4),
    ShipingAmount DECIMAL(18,4),
    DiscountPer DECIMAL(18,4),
    DiscountAmount DECIMAL(18,4),
    GrossAmount DECIMAL(18,4),
    Remark NVARCHAR(MAX),
    Remark1 NVARCHAR(MAX),
    Remark2 NVARCHAR(MAX),
    Remark3 NVARCHAR(MAX),
    IsSave BIT,
    IsReturn BIT DEFAULT 0,
    IsPartiaReturn BIT DEFAULT 0,
    RefReturnSalesID INT,
    RefReturnSalesInvoiceNo INT,
    IsActive BIT DEFAULT 0,
    IsCoupon BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    IsDelete BIT DEFAULT 0,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);




/* =========================================
   Sales Transation Master
========================================= */
CREATE TABLE SalesTransactionMaster (
    SalesTransactionId INT IDENTITY PRIMARY KEY,
    SalesMasterId INT,
    SalesInvoiceNo INT,
    SalesInvoiceNoPrefix NVARCHAR(100),
    SalesDate INT,
    CustomerId INT,
    CategoryId INT,
    SubCategoryId INT,
    ItemId INT,
    HSNCode NVARCHAR(100),
    QTY DECIMAL(18,4),
    Rate DECIMAL(18,4),
    DiscountPer DECIMAL(18,4),
    DiscountAmount DECIMAL(18,4),
    CGST DECIMAL(18,4),
    SGST DECIMAL(18,4),
    IGST DECIMAL(18,4),
    GST DECIMAL(18,4),
    IsIGST BIT DEFAULT 0,
    TaxPer DECIMAL(18,4),
    TaxAmount DECIMAL(18,4),
    ITaxPer DECIMAL(18,4),
    ITaxAmount DECIMAL(18,4),
    TotalAmount DECIMAL(18,4),
    ActualRate DECIMAL(18,4),
    Remark NVARCHAR(MAX),
    IsSave BIT,
    IsPrint BIT,
    IsReturn BIT DEFAULT 0,
    RefReturnSalesTranID INT,
    RefReturnSalesInvoiceNo INT,
    InventoryId INT,
    PurchaseInvoiceNo INT, 
    ActualCostRate DECIMAL(18,4),
    SupplierId INT,
    IsActive BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    IsDelete BIT DEFAULT 0,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);

/* =========================================
   Sales Payment Master
========================================= */
CREATE TABLE SalesPaymentMaster (
    SalesPaymentId INT IDENTITY PRIMARY KEY,
    SalesMasterId INT,
    SalesInvoiceNo INT,
    SalesInvoiceNoPrefix NVARCHAR(100),
    SalesDate INT,
    CustomerId INT,
    PaymentTypeId INT,
    TotalAmount DECIMAL(18,4),
    Remark NVARCHAR(MAX),
    Remark1 NVARCHAR(MAX),
    Remark2 NVARCHAR(MAX),
    IsSave BIT,
    IsActive BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    IsDelete BIT DEFAULT 0,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);






/* =========================================
   Purchase Return Master
========================================= */
CREATE TABLE PurchaseReturnMaster (
    PurchaseReturnId INT IDENTITY PRIMARY KEY,
    PurchaseReturnInvoiceNo INT,
    PurchaseReturnInvoiceNoPrefix NVARCHAR(100),
    PurchaseReturnDate INT,
    SupplierId INT,
    SupplierReturnInvoiceNo NVARCHAR(MAX),
    SupplierEntryDate INT,
    UniqueGUID NVARCHAR(MAX),
    GRNO NVARCHAR(100),
    TotalItem DECIMAL(18,4),
    TotalAmount DECIMAL(18,4),
    Remark NVARCHAR(MAX),
    IsSave BIT,
    IsActive BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    IsDelete BIT DEFAULT 0,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);


/* =========================================
   Purchase Return Transaction Master
========================================= */
CREATE TABLE PurchaseReturnTransactionMaster (
    PurchaseReturnTransactionId INT IDENTITY PRIMARY KEY,
    PurchaseReturnMasterId INT,
    PurchaseReturnInvoiceNo INT,
    PurchaseReturnInvoiceNoPrefix NVARCHAR(100),
    PurchaseReturnDate INT,
    SupplierId INT,
    CategoryId INT,
    SubCategoryId INT,
    ItemId INT,
    QTY DECIMAL(18,4),
    CostRate DECIMAL(18,4),
    CostPrice DECIMAL(18,4),
    SellRate DECIMAL(18,4),
    SellPrice DECIMAL(18,4),
    DiscountPer DECIMAL(18,4),
    DiscountAmount DECIMAL(18,4),
    MarkUpPer DECIMAL(18,4),
    MarkUpAmount DECIMAL(18,4),
    CGST DECIMAL(18,4),
    SGST DECIMAL(18,4),
    IGST DECIMAL(18,4),
    GST DECIMAL(18,4),
    IsIGST BIT DEFAULT 0,
    TaxPer DECIMAL(18,4),
    TaxAmount DECIMAL(18,4),
    ITaxPer DECIMAL(18,4),
    ITaxAmount DECIMAL(18,4),
    TotalAmount DECIMAL(18,4),
    ActualCostRate DECIMAL(18,4),
    Remark NVARCHAR(MAX),
    IsSave BIT,
    IsActive BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    IsDelete BIT DEFAULT 0,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);

/* =========================================
   Purchase Return Payment Master
========================================= */
CREATE TABLE PurchaseReturnPaymentMaster (
    PurchaseReturnPaymentId INT IDENTITY PRIMARY KEY,
    PurchaseReturnMasterId INT,
    PurchaseReturnInvoiceNo INT,
    PurchaseReturnInvoiceNoPrefix NVARCHAR(100),
    PurchaseReturnDate INT,
    SupplierId INT,
    PaymentTypeId INT,
    TotalAmount DECIMAL(18,4),
    Remark NVARCHAR(MAX),
    IsSave BIT,
    IsActive BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    IsDelete BIT DEFAULT 0,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);



/* =========================================
   Sales Return Master
========================================= */
CREATE TABLE SalesReturnMaster (
    SalesReturnId INT IDENTITY PRIMARY KEY,
    SalesReturnInvoiceNo INT,
    SalesReturnInvoiceNoPrefix NVARCHAR(100),
    SalesReturnDate INT,
    CustomerId INT,
    UniqueGUID NVARCHAR(MAX),
    GRNO NVARCHAR(100),
    TotalItem DECIMAL(18,4),
    TotalAmount DECIMAL(18,4),
    Remark NVARCHAR(MAX),
    Remark1 NVARCHAR(MAX),
    Remark2 NVARCHAR(MAX),
    Remark3 NVARCHAR(MAX),
    IsSave BIT,
    IsReturn BIT DEFAULT 0,
    IsPartiaReturn BIT DEFAULT 0,
    RefReturnSalesID INT,
    RefReturnSalesInvoiceNo INT,
    IsActive BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    IsDelete BIT DEFAULT 0,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);




/* =========================================
   Sales Return Transation Master
========================================= */
CREATE TABLE SalesReturnTransactionMaster (
    SalesReturnTransactionId INT IDENTITY PRIMARY KEY,
    SalesReturnMasterId INT,
    SalesReturnInvoiceNo INT,
    SalesReturnInvoiceNoPrefix NVARCHAR(100),
    SalesReturnDate INT,
    CustomerId INT,
    CategoryId INT,
    SubCategoryId INT,
    ItemId INT,
    HSNCode NVARCHAR(100),
    QTY DECIMAL(18,4),
    Rate DECIMAL(18,4),
    DiscountPer DECIMAL(18,4),
    DiscountAmount DECIMAL(18,4),
    CGST DECIMAL(18,4),
    SGST DECIMAL(18,4),
    IGST DECIMAL(18,4),
    GST DECIMAL(18,4),
    IsIGST BIT DEFAULT 0,
    TaxPer DECIMAL(18,4),
    TaxAmount DECIMAL(18,4),
    ITaxPer DECIMAL(18,4),
    ITaxAmount DECIMAL(18,4),
    TotalAmount DECIMAL(18,4),
    ActualRate DECIMAL(18,4),
    Remark NVARCHAR(MAX),
    IsSave BIT,
    IsPrint BIT,
    IsReturn BIT DEFAULT 0,
    RefReturnSalesTranID INT,
    RefReturnSalesInvoiceNo INT,
    InventoryId INT,
    PurchaseInvoiceNo INT, 
    ActualCostRate DECIMAL(18,4),
    SupplierId INT,
    IsActive BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    IsDelete BIT DEFAULT 0,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);

/* =========================================
   Sales Return Payment Master
========================================= */
CREATE TABLE SalesReturnPaymentMaster (
    SalesReturnPaymentId INT IDENTITY PRIMARY KEY,
    SalesReturnMasterId INT,
    SalesReturnInvoiceNo INT,
    SalesReturnInvoiceNoPrefix NVARCHAR(100),
    SalesReturnDate INT,
    CustomerId INT,
    PaymentTypeId INT,
    TotalAmount DECIMAL(18,4),
    Remark NVARCHAR(MAX),
    Remark1 NVARCHAR(MAX),
    Remark2 NVARCHAR(MAX),
    IsSave BIT,
    IsActive BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    IsDelete BIT DEFAULT 0,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);



/* =========================================
   Inventory Inward Master
========================================= */
CREATE TABLE InventoryInwardMaster (
    InventoryInwardId INT IDENTITY PRIMARY KEY,
    PurchaseInvoiceNo INT,
    PurchaseInvoiceNoPrefix NVARCHAR(100),
    PurchaseDate INT,
    SupplierId INT,
    CategoryId INT,
    SubCategoryId INT,
    ItemId INT,
    HSNCode NVARCHAR(100),
    QTY DECIMAL(18,4),
    CostRate DECIMAL(18,4),
    SellRate DECIMAL(18,4),
    MRP DECIMAL(18,4),
    Remark NVARCHAR(MAX),
    IsActive BIT DEFAULT 0,
    TranType Nvarchar(100), --Purchase,Sale,PurchaseReturn,SaleReturn
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    IsDelete BIT DEFAULT 0,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);


/* =========================================
   Inventory Outward Master
========================================= */
CREATE TABLE InventoryOutwardMaster (
    InventoryOutwardId INT IDENTITY PRIMARY KEY,
    SalesInvoiceNo INT,
    SalesInvoiceNoPrefix NVARCHAR(100),
    SalesDate INT,
    CustomerId INT,
    CategoryId INT,
    SubCategoryId INT,
    ItemId INT,
    HSNCode NVARCHAR(100),
    QTY DECIMAL(18,4),
    CostRate DECIMAL(18,4),
    SellRate DECIMAL(18,4),
    MRP DECIMAL(18,4),
    Remark NVARCHAR(MAX),
    IsActive BIT DEFAULT 0,
    TranType Nvarchar(100), --Purchase,Sale,PurchaseReturn,SaleReturn
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    IsDelete BIT DEFAULT 0,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);



/* =========================================
   Inventory Master
========================================= */
CREATE TABLE InventoryMaster (
    InventoryId INT IDENTITY PRIMARY KEY,
    CategoryId INT,
    SubCategoryId INT,
    ItemId INT,
    ItemBarcode NVARCHAR(MAX),
    HSNCode NVARCHAR(100),
    QTY DECIMAL(18,4),
    CostRate DECIMAL(18,4),
    SellRate DECIMAL(18,4),
    ActualCostRate DECIMAL(18,4),
    MRP DECIMAL(18,4),
    Remark NVARCHAR(MAX),
    IsActive BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    IsDelete BIT DEFAULT 0,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);


/* =========================================
   Inventory Log Master
========================================= */
CREATE TABLE InventorylogMaster (
    InventoryLogId INT IDENTITY PRIMARY KEY,
    CategoryId INT,
    SubCategoryId INT,
    ItemId INT,
    ItemBarcode NVARCHAR(MAX),
    HSNCode NVARCHAR(100),
    QTY DECIMAL(18,4),
    CostRate DECIMAL(18,4),
    SellRate DECIMAL(18,4),
    ActualCostRate DECIMAL(18,4),
    MRP DECIMAL(18,4),
    TranType Nvarchar(100), --Purchase,Sale,PurchaseReturn,SaleReturn
    CreatedDate DATETIME DEFAULT GETDATE()
);



/* =========================================
   Sales ECOM Master
========================================= */
CREATE TABLE SalesECOMMaster (
    SalesECOMId INT IDENTITY PRIMARY KEY,
    SalesECOMInvoiceNo INT,
    SalesECOMInvoiceNoPrefix NVARCHAR(100),
    SalesECOMDate INT,
    CustomerId INT,
    StatusId INT,
    UniqueGUID NVARCHAR(MAX),
    GRNO NVARCHAR(100),
    TotalItem DECIMAL(18,4),
    TotalAmount DECIMAL(18,4),
    TaxPer DECIMAL(18,4),
    TaxAmount DECIMAL(18,4),
    ShipingPer DECIMAL(18,4),
    ShipingAmount DECIMAL(18,4),
    DiscountPer DECIMAL(18,4),
    DiscountAmount DECIMAL(18,4),
    GrossAmount DECIMAL(18,4),
    Remark NVARCHAR(MAX),
    Remark1 NVARCHAR(MAX),
    Remark2 NVARCHAR(MAX),
    Remark3 NVARCHAR(MAX),
    IsSave BIT,
    IsReturn BIT DEFAULT 0,
    IsPartiaReturn BIT DEFAULT 0,
    RefReturnSalesID INT,
    RefReturnSalesInvoiceNo INT,
    IsActive BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    IsDelete BIT DEFAULT 0,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);




/* =========================================
   Sales ECOM Transation Master
========================================= */
CREATE TABLE SalesECOMTransactionMaster (
    SalesECOMTransactionId INT IDENTITY PRIMARY KEY,
    SalesECOMMasterId INT,
    SalesECOMInvoiceNo INT,
    SalesECOMInvoiceNoPrefix NVARCHAR(100),
    SalesECOMDate INT,
    CustomerId INT,
    StatusId INT,
    CategoryId INT,
    SubCategoryId INT,
    ItemId INT,
    HSNCode NVARCHAR(100),
    QTY DECIMAL(18,4),
    Rate DECIMAL(18,4),
    DiscountPer DECIMAL(18,4),
    DiscountAmount DECIMAL(18,4),
    CGST DECIMAL(18,4),
    SGST DECIMAL(18,4),
    IGST DECIMAL(18,4),
    GST DECIMAL(18,4),
    IsIGST BIT DEFAULT 0,
    TaxPer DECIMAL(18,4),
    TaxAmount DECIMAL(18,4),
    ITaxPer DECIMAL(18,4),
    ITaxAmount DECIMAL(18,4),
    TotalAmount DECIMAL(18,4),
    ActualRate DECIMAL(18,4),
    Remark NVARCHAR(MAX),
    IsSave BIT,
    IsPrint BIT,
    IsReturn BIT DEFAULT 0,
    RefReturnSalesTranID INT,
    RefReturnSalesInvoiceNo INT,
    InventoryId INT,
    PurchaseInvoiceNo INT, 
    ActualCostRate DECIMAL(18,4),
    SupplierId INT,
    IsActive BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    IsDelete BIT DEFAULT 0,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);

/* =========================================
   Sales ECOM Payment Master
========================================= */
CREATE TABLE SalesECOMPaymentMaster (
    SalesECOMPaymentId INT IDENTITY PRIMARY KEY,
    SalesECOMMasterId INT,
    SalesECOMInvoiceNo INT,
    SalesECOMInvoiceNoPrefix NVARCHAR(100),
    SalesECOMDate INT,
    CustomerId INT,
    PaymentTypeId INT,
    TotalAmount DECIMAL(18,4),
    Remark NVARCHAR(MAX),
    IsSave BIT,
    IsActive BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedDate DATETIME,
    UpdatedBy INT,
    IsDelete BIT DEFAULT 0,
    CompId INT,
    YearId INT,
    DeletedDate DATETIME,
    DeletedBy INT
);


/* =========================================
   Sales ECOM Payment Master
========================================= */


/* =========================================
   Sales ECOM Payment Master
========================================= */

