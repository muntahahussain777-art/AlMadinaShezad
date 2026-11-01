-- ============================================================
-- AL NOOR / DiselPetrolPump — DEPLOY EMPTY DATA (SQLite)
-- ============================================================
-- Kaam: saara business data empty → naya client / deploy ke liye
-- Schema (tables/columns) SAME rahegi — DROP TABLE nahi
--
-- RAHEGA (KEEP):
--   tblUser          → login / password
--   License          → software license (agar hai)
--   RolePermissions  → role access settings (agar chahiye empty to neeche uncomment)
--
-- HATAYA JAYEGA (WIPE):
--   customers, dealers, sales, stock, bank, expense, ledgers, audit, sync, etc.
--
-- DB path (ye software AppData use karti hai):
--   %LOCALAPPDATA%\DiselPetrolPump\DiselPetrolPumpAlMadina.db
--
-- Pehle BACKUP lo, phir ye script DB Browser for SQLite / SQLite CLI mein chalao.
-- ============================================================

PRAGMA foreign_keys = OFF;
BEGIN TRANSACTION;

-- ---------- child / transaction tables pehle ----------
DELETE FROM PetrolAdd;
DELETE FROM PetrolTransactions;
DELETE FROM AddStock;
DELETE FROM StockDiesel;
DELETE FROM BankTransactions;
DELETE FROM DieselLedger;
DELETE FROM DieselLedgerCredit;
DELETE FROM DieselLedgerDebit;
DELETE FROM DealerToDealer;
DELETE FROM CustomerToCustomer;
DELETE FROM Expensetable;
DELETE FROM AuditLog;
DELETE FROM SyncTombstone;
DELETE FROM Banks;

-- agar ye extra/legacy tables maujood hain (dosri copy / purana software)
DELETE FROM DiselPetrolPump;

-- ---------- master tables ----------
DELETE FROM AddCustomer;
DELETE FROM AddDealer;

-- RolePermissions empty chahiye to is line ko UNCOMMENT karo:
-- DELETE FROM RolePermissions;

-- ---------- autoincrement IDs reset (sirf wiped tables) ----------
DELETE FROM sqlite_sequence
WHERE name IN (
    'PetrolAdd',
    'PetrolTransactions',
    'AddStock',
    'StockDiesel',
    'BankTransactions',
    'DieselLedger',
    'DieselLedgerCredit',
    'DieselLedgerDebit',
    'DealerToDealer',
    'CustomerToCustomer',
    'Expensetable',
    'AuditLog',
    'SyncTombstone',
    'Banks',
    'AddCustomer',
    'AddDealer',
    'DiselPetrolPump'
);

COMMIT;
PRAGMA foreign_keys = ON;

-- ---------- verify (optional) ----------
-- SELECT 'AddCustomer' AS T, COUNT(*) AS C FROM AddCustomer
-- UNION ALL SELECT 'AddDealer', COUNT(*) FROM AddDealer
-- UNION ALL SELECT 'PetrolAdd', COUNT(*) FROM PetrolAdd
-- UNION ALL SELECT 'AddStock', COUNT(*) FROM AddStock
-- UNION ALL SELECT 'StockDiesel', COUNT(*) FROM StockDiesel
-- UNION ALL SELECT 'BankTransactions', COUNT(*) FROM BankTransactions
-- UNION ALL SELECT 'Expensetable', COUNT(*) FROM Expensetable
-- UNION ALL SELECT 'tblUser', COUNT(*) FROM tblUser;
--
-- tblUser count > 0 hona chahiye (login same rahe)
