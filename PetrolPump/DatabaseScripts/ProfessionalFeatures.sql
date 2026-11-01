-- Zaib Petroleum Professional Features
-- Purane database restore ke baad bhi app khud ye tables/columns add kar deti hai.
-- Manual run optional hai.

CREATE TABLE IF NOT EXISTS AuditLog (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ActionTime TEXT NOT NULL,
    UserName TEXT,
    UserRole TEXT,
    ActionType TEXT,
    TableName TEXT,
    RecordId TEXT,
    QuerySummary TEXT,
    Details TEXT
);

CREATE TABLE IF NOT EXISTS RolePermissions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RoleName TEXT NOT NULL,
    FormKey TEXT NOT NULL,
    CanAccess INTEGER NOT NULL DEFAULT 1,
    UNIQUE(RoleName, FormKey)
);

-- Purane DB ke liye (agar column na ho):
-- ALTER TABLE BankTransactions ADD COLUMN BankName TEXT;
-- ALTER TABLE tblUser ADD COLUMN uRole TEXT DEFAULT 'Admin';
