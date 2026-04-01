-- =============================================================================
-- Contoso Bank — Seed Data Script
-- Populates Azure SQL Database with realistic banking demo data.
-- Idempotent: skips inserts if Accounts table already has data.
-- =============================================================================

SET NOCOUNT ON;

-- Guard: only seed if no accounts exist
IF EXISTS (SELECT 1 FROM [Accounts])
BEGIN
    PRINT 'Database already seeded — skipping.';
    RETURN;
END

DECLARE @now DATETIMEOFFSET = SYSUTCDATETIME();

-- =============================================================================
-- Accounts: 2 checking, 1 savings, 1 credit, 1 business
-- =============================================================================
SET IDENTITY_INSERT [Accounts] ON;

INSERT INTO [Accounts] ([Id], [AccountNumber], [AccountName], [AccountType], [Balance], [Currency], [CreatedAt])
VALUES
    (1, N'CHK-100001', N'Primary Checking',     N'Checking', 12450.75, N'USD', DATEADD(MONTH, -6, @now)),
    (2, N'CHK-100002', N'Joint Checking',        N'Checking',  8320.50, N'USD', DATEADD(MONTH, -4, @now)),
    (3, N'SAV-200001', N'High-Yield Savings',    N'Savings',  45000.00, N'USD', DATEADD(MONTH, -12, @now)),
    (4, N'CRD-300001', N'Platinum Credit Card',  N'Credit',    2150.30, N'USD', DATEADD(MONTH, -8, @now)),
    (5, N'BIZ-400001', N'Business Account',      N'Checking', 67890.25, N'USD', DATEADD(MONTH, -3, @now));

SET IDENTITY_INSERT [Accounts] OFF;

-- =============================================================================
-- Transactions: 100+ realistic transactions across the past 30 days
-- Covers all 5 accounts with a mix of credits and debits.
-- Categories: Groceries, Utilities, Payroll, Rent, Entertainment, Dining,
--             Transport, Healthcare, Insurance, Subscription
-- =============================================================================
SET IDENTITY_INSERT [Transactions] ON;

INSERT INTO [Transactions] ([Id], [AccountId], [Type], [Amount], [Description], [Category], [Timestamp], [Status])
VALUES
    -- =========================================================================
    -- Account 1: Primary Checking (CHK-100001) — 25 transactions
    -- =========================================================================
    (  1, 1, N'Debit',   87.43, N'Whole Foods Market',             N'Groceries',     DATEADD(DAY, -1, @now),  N'Completed'),
    (  2, 1, N'Debit',   42.17, N'Trader Joe''s',                  N'Groceries',     DATEADD(DAY, -3, @now),  N'Completed'),
    (  3, 1, N'Debit',  156.89, N'Costco Wholesale',               N'Groceries',     DATEADD(DAY, -7, @now),  N'Completed'),
    (  4, 1, N'Debit',  134.50, N'Electric Bill - ConEd',          N'Utilities',     DATEADD(DAY, -5, @now),  N'Completed'),
    (  5, 1, N'Debit',   78.90, N'Internet - Comcast',             N'Utilities',     DATEADD(DAY, -10, @now), N'Completed'),
    (  6, 1, N'Credit', 4250.00, N'Salary Deposit - Contoso Ltd',  N'Payroll',       DATEADD(DAY, -1, @now),  N'Completed'),
    (  7, 1, N'Credit', 4250.00, N'Salary Deposit - Contoso Ltd',  N'Payroll',       DATEADD(DAY, -15, @now), N'Completed'),
    (  8, 1, N'Debit', 1850.00, N'Monthly Rent Payment',           N'Rent',          DATEADD(DAY, -1, @now),  N'Completed'),
    (  9, 1, N'Debit',   15.99, N'Netflix Subscription',           N'Entertainment', DATEADD(DAY, -12, @now), N'Completed'),
    ( 10, 1, N'Debit',    9.99, N'Spotify Premium',                N'Entertainment', DATEADD(DAY, -8, @now),  N'Completed'),
    ( 11, 1, N'Debit',    5.75, N'Starbucks Coffee',               N'Dining',        DATEADD(DAY, -2, @now),  N'Completed'),
    ( 12, 1, N'Debit',   34.50, N'Chipotle Mexican Grill',         N'Dining',        DATEADD(DAY, -4, @now),  N'Completed'),
    ( 13, 1, N'Debit',  127.85, N'The Capital Grille',             N'Dining',        DATEADD(DAY, -9, @now),  N'Completed'),
    ( 14, 1, N'Debit',   18.50, N'Uber Ride',                      N'Transport',     DATEADD(DAY, -2, @now),  N'Completed'),
    ( 15, 1, N'Debit',   33.00, N'Metro Card Refill',              N'Transport',     DATEADD(DAY, -14, @now), N'Completed'),
    ( 16, 1, N'Debit',   52.40, N'Gas Station - Shell',            N'Transport',     DATEADD(DAY, -11, @now), N'Completed'),
    ( 17, 1, N'Debit',   25.00, N'Pharmacy - CVS',                 N'Healthcare',    DATEADD(DAY, -6, @now),  N'Completed'),
    ( 18, 1, N'Debit',   40.00, N'Doctor Visit Copay',             N'Healthcare',    DATEADD(DAY, -20, @now), N'Completed'),
    ( 19, 1, N'Debit',  185.00, N'Auto Insurance Premium',         N'Insurance',     DATEADD(DAY, -16, @now), N'Completed'),
    ( 20, 1, N'Debit',   54.99, N'Adobe Creative Cloud',           N'Subscription',  DATEADD(DAY, -13, @now), N'Completed'),
    ( 21, 1, N'Debit',   12.99, N'Microsoft 365',                  N'Subscription',  DATEADD(DAY, -18, @now), N'Completed'),
    ( 22, 1, N'Debit',   63.20, N'Safeway',                        N'Groceries',     DATEADD(DAY, -22, @now), N'Completed'),
    ( 23, 1, N'Debit',   28.45, N'DoorDash Delivery',              N'Dining',        DATEADD(DAY, -25, @now), N'Completed'),
    ( 24, 1, N'Debit',   22.00, N'Parking Fee',                    N'Transport',     DATEADD(DAY, -28, @now), N'Completed'),
    ( 25, 1, N'Credit', 1500.00, N'Bonus Payment',                 N'Payroll',       DATEADD(DAY, -19, @now), N'Pending'),

    -- =========================================================================
    -- Account 2: Joint Checking (CHK-100002) — 22 transactions
    -- =========================================================================
    ( 26, 2, N'Credit', 3800.00, N'Salary Deposit - Contoso Ltd',  N'Payroll',       DATEADD(DAY, -1, @now),  N'Completed'),
    ( 27, 2, N'Credit', 3800.00, N'Salary Deposit - Contoso Ltd',  N'Payroll',       DATEADD(DAY, -15, @now), N'Completed'),
    ( 28, 2, N'Debit',   94.32, N'Costco Wholesale',               N'Groceries',     DATEADD(DAY, -2, @now),  N'Completed'),
    ( 29, 2, N'Debit',   67.81, N'Whole Foods Market',             N'Groceries',     DATEADD(DAY, -6, @now),  N'Completed'),
    ( 30, 2, N'Debit',   38.90, N'Trader Joe''s',                  N'Groceries',     DATEADD(DAY, -9, @now),  N'Completed'),
    ( 31, 2, N'Debit',  112.45, N'Water & Sewer',                  N'Utilities',     DATEADD(DAY, -4, @now),  N'Completed'),
    ( 32, 2, N'Debit',   89.00, N'Gas Bill - National Grid',       N'Utilities',     DATEADD(DAY, -8, @now),  N'Completed'),
    ( 33, 2, N'Debit', 1650.00, N'Monthly Rent Payment',           N'Rent',          DATEADD(DAY, -1, @now),  N'Completed'),
    ( 34, 2, N'Debit',  150.00, N'Parking Space Rental',           N'Rent',          DATEADD(DAY, -1, @now),  N'Completed'),
    ( 35, 2, N'Debit',   24.99, N'Movie Tickets - AMC',            N'Entertainment', DATEADD(DAY, -5, @now),  N'Completed'),
    ( 36, 2, N'Debit',   85.00, N'Concert Tickets',                N'Entertainment', DATEADD(DAY, -17, @now), N'Completed'),
    ( 37, 2, N'Debit',    6.25, N'Starbucks Coffee',               N'Dining',        DATEADD(DAY, -3, @now),  N'Completed'),
    ( 38, 2, N'Debit',   41.30, N'DoorDash Delivery',              N'Dining',        DATEADD(DAY, -7, @now),  N'Completed'),
    ( 39, 2, N'Debit',   14.50, N'Uber Ride',                      N'Transport',     DATEADD(DAY, -10, @now), N'Completed'),
    ( 40, 2, N'Debit',   48.75, N'Gas Station - Shell',            N'Transport',     DATEADD(DAY, -13, @now), N'Completed'),
    ( 41, 2, N'Debit',  120.00, N'Dental Cleaning',                N'Healthcare',    DATEADD(DAY, -21, @now), N'Completed'),
    ( 42, 2, N'Debit',  320.00, N'Health Insurance Premium',       N'Insurance',     DATEADD(DAY, -11, @now), N'Completed'),
    ( 43, 2, N'Debit',   14.99, N'Netflix Subscription',           N'Entertainment', DATEADD(DAY, -12, @now), N'Completed'),
    ( 44, 2, N'Debit',   55.60, N'Safeway',                        N'Groceries',     DATEADD(DAY, -16, @now), N'Completed'),
    ( 45, 2, N'Credit',  750.00, N'Freelance Payment',             N'Payroll',       DATEADD(DAY, -22, @now), N'Completed'),
    ( 46, 2, N'Debit',   33.00, N'Metro Card Refill',              N'Transport',     DATEADD(DAY, -24, @now), N'Completed'),
    ( 47, 2, N'Debit',   19.99, N'AWS Monthly Bill',               N'Subscription',  DATEADD(DAY, -26, @now), N'Pending'),

    -- =========================================================================
    -- Account 3: High-Yield Savings (SAV-200001) — 18 transactions
    -- =========================================================================
    ( 48, 3, N'Credit', 2000.00, N'Salary Deposit - Contoso Ltd',  N'Payroll',       DATEADD(DAY, -1, @now),  N'Completed'),
    ( 49, 3, N'Credit', 2000.00, N'Salary Deposit - Contoso Ltd',  N'Payroll',       DATEADD(DAY, -15, @now), N'Completed'),
    ( 50, 3, N'Credit', 5000.00, N'Bonus Payment',                 N'Payroll',       DATEADD(DAY, -8, @now),  N'Completed'),
    ( 51, 3, N'Credit',   45.23, N'Interest Payment',              N'Payroll',       DATEADD(DAY, -1, @now),  N'Completed'),
    ( 52, 3, N'Credit',   44.89, N'Interest Payment',              N'Payroll',       DATEADD(DAY, -30, @now), N'Completed'),
    ( 53, 3, N'Debit',  500.00, N'Transfer to Checking',           N'Transport',     DATEADD(DAY, -3, @now),  N'Completed'),
    ( 54, 3, N'Debit', 1000.00, N'Transfer to Checking',           N'Transport',     DATEADD(DAY, -10, @now), N'Completed'),
    ( 55, 3, N'Debit',  250.00, N'Transfer to Joint Checking',     N'Transport',     DATEADD(DAY, -14, @now), N'Completed'),
    ( 56, 3, N'Credit', 3000.00, N'Tax Refund Deposit',            N'Payroll',       DATEADD(DAY, -20, @now), N'Completed'),
    ( 57, 3, N'Debit', 2000.00, N'Investment Transfer',            N'Transport',     DATEADD(DAY, -22, @now), N'Completed'),
    ( 58, 3, N'Credit', 1500.00, N'Freelance Payment',             N'Payroll',       DATEADD(DAY, -25, @now), N'Completed'),
    ( 59, 3, N'Credit', 2500.00, N'Salary Deposit - Contoso Ltd',  N'Payroll',       DATEADD(DAY, -29, @now), N'Completed'),
    ( 60, 3, N'Debit',  300.00, N'Emergency Fund Withdrawal',      N'Healthcare',    DATEADD(DAY, -18, @now), N'Completed'),
    ( 61, 3, N'Credit',  200.00, N'Cashback Reward',               N'Payroll',       DATEADD(DAY, -12, @now), N'Completed'),
    ( 62, 3, N'Debit',  750.00, N'Transfer to Business Account',   N'Transport',     DATEADD(DAY, -5, @now),  N'Completed'),
    ( 63, 3, N'Credit', 1000.00, N'Dividend Payment',              N'Payroll',       DATEADD(DAY, -16, @now), N'Completed'),
    ( 64, 3, N'Debit',  100.00, N'Charity Donation',               N'Entertainment', DATEADD(DAY, -27, @now), N'Completed'),
    ( 65, 3, N'Credit',  500.00, N'Refund - Insurance Overpayment',N'Insurance',     DATEADD(DAY, -6, @now),  N'Pending'),

    -- =========================================================================
    -- Account 4: Platinum Credit Card (CRD-300001) — 23 transactions
    -- =========================================================================
    ( 66, 4, N'Debit',   72.15, N'Whole Foods Market',             N'Groceries',     DATEADD(DAY, -1, @now),  N'Completed'),
    ( 67, 4, N'Debit',   31.44, N'Trader Joe''s',                  N'Groceries',     DATEADD(DAY, -4, @now),  N'Completed'),
    ( 68, 4, N'Debit',  189.99, N'Costco Wholesale',               N'Groceries',     DATEADD(DAY, -8, @now),  N'Completed'),
    ( 69, 4, N'Debit',  134.50, N'Electric Bill - ConEd',          N'Utilities',     DATEADD(DAY, -3, @now),  N'Completed'),
    ( 70, 4, N'Debit',   65.00, N'Internet - Comcast',             N'Utilities',     DATEADD(DAY, -10, @now), N'Completed'),
    ( 71, 4, N'Credit', 2000.00, N'Credit Card Payment - Thank You',N'Payroll',      DATEADD(DAY, -6, @now),  N'Completed'),
    ( 72, 4, N'Debit',   15.99, N'Netflix Subscription',           N'Entertainment', DATEADD(DAY, -12, @now), N'Completed'),
    ( 73, 4, N'Debit',    9.99, N'Spotify Premium',                N'Entertainment', DATEADD(DAY, -12, @now), N'Completed'),
    ( 74, 4, N'Debit',   35.00, N'Movie Tickets - AMC',            N'Entertainment', DATEADD(DAY, -15, @now), N'Completed'),
    ( 75, 4, N'Debit',    7.50, N'Starbucks Coffee',               N'Dining',        DATEADD(DAY, -1, @now),  N'Completed'),
    ( 76, 4, N'Debit',   22.80, N'Chipotle Mexican Grill',         N'Dining',        DATEADD(DAY, -5, @now),  N'Completed'),
    ( 77, 4, N'Debit',   95.40, N'The Capital Grille',             N'Dining',        DATEADD(DAY, -9, @now),  N'Completed'),
    ( 78, 4, N'Debit',   45.30, N'DoorDash Delivery',              N'Dining',        DATEADD(DAY, -14, @now), N'Completed'),
    ( 79, 4, N'Debit',   32.50, N'Uber Ride',                      N'Transport',     DATEADD(DAY, -2, @now),  N'Completed'),
    ( 80, 4, N'Debit',   55.00, N'Gas Station - Shell',            N'Transport',     DATEADD(DAY, -11, @now), N'Completed'),
    ( 81, 4, N'Debit',   12.00, N'Parking Fee',                    N'Transport',     DATEADD(DAY, -7, @now),  N'Completed'),
    ( 82, 4, N'Debit',   40.00, N'Doctor Visit Copay',             N'Healthcare',    DATEADD(DAY, -18, @now), N'Completed'),
    ( 83, 4, N'Debit',   15.50, N'Pharmacy - CVS',                 N'Healthcare',    DATEADD(DAY, -21, @now), N'Completed'),
    ( 84, 4, N'Debit',  185.00, N'Auto Insurance Premium',         N'Insurance',     DATEADD(DAY, -16, @now), N'Completed'),
    ( 85, 4, N'Debit',  320.00, N'Health Insurance Premium',       N'Insurance',     DATEADD(DAY, -16, @now), N'Completed'),
    ( 86, 4, N'Debit',   54.99, N'Adobe Creative Cloud',           N'Subscription',  DATEADD(DAY, -13, @now), N'Completed'),
    ( 87, 4, N'Debit',   12.99, N'Microsoft 365',                  N'Subscription',  DATEADD(DAY, -13, @now), N'Completed'),
    ( 88, 4, N'Debit',   48.30, N'Safeway',                        N'Groceries',     DATEADD(DAY, -23, @now), N'Pending'),

    -- =========================================================================
    -- Account 5: Business Account (BIZ-400001) — 24 transactions
    -- =========================================================================
    ( 89, 5, N'Credit', 5500.00, N'Salary Deposit - Contoso Ltd',  N'Payroll',       DATEADD(DAY, -1, @now),  N'Completed'),
    ( 90, 5, N'Credit', 5500.00, N'Salary Deposit - Contoso Ltd',  N'Payroll',       DATEADD(DAY, -15, @now), N'Completed'),
    ( 91, 5, N'Credit', 8500.00, N'Client Payment - Acme Corp',    N'Payroll',       DATEADD(DAY, -7, @now),  N'Completed'),
    ( 92, 5, N'Credit', 3200.00, N'Client Payment - Globex Inc',   N'Payroll',       DATEADD(DAY, -14, @now), N'Completed'),
    ( 93, 5, N'Credit', 6750.00, N'Client Payment - Wayne Enterprises', N'Payroll',  DATEADD(DAY, -21, @now), N'Completed'),
    ( 94, 5, N'Debit', 2400.00, N'Office Rent Payment',            N'Rent',          DATEADD(DAY, -1, @now),  N'Completed'),
    ( 95, 5, N'Debit',  350.00, N'Parking Space Rental',           N'Rent',          DATEADD(DAY, -1, @now),  N'Completed'),
    ( 96, 5, N'Debit',  245.00, N'Electric Bill - ConEd',          N'Utilities',     DATEADD(DAY, -5, @now),  N'Completed'),
    ( 97, 5, N'Debit',  189.00, N'Internet - Comcast Business',    N'Utilities',     DATEADD(DAY, -10, @now), N'Completed'),
    ( 98, 5, N'Debit',  499.00, N'AWS Monthly Bill',               N'Subscription',  DATEADD(DAY, -3, @now),  N'Completed'),
    ( 99, 5, N'Debit',  299.00, N'Adobe Creative Cloud Team',      N'Subscription',  DATEADD(DAY, -13, @now), N'Completed'),
    (100, 5, N'Debit',   79.99, N'Microsoft 365 Business',         N'Subscription',  DATEADD(DAY, -13, @now), N'Completed'),
    (101, 5, N'Debit',  156.00, N'Office Supplies - Staples',      N'Groceries',     DATEADD(DAY, -6, @now),  N'Completed'),
    (102, 5, N'Debit',   89.50, N'Business Lunch - Client Meeting',N'Dining',        DATEADD(DAY, -4, @now),  N'Completed'),
    (103, 5, N'Debit',  215.00, N'Team Dinner',                    N'Dining',        DATEADD(DAY, -11, @now), N'Completed'),
    (104, 5, N'Debit',   65.00, N'Uber Business Ride',             N'Transport',     DATEADD(DAY, -2, @now),  N'Completed'),
    (105, 5, N'Debit',  128.50, N'Gas Station - Shell Fleet',      N'Transport',     DATEADD(DAY, -9, @now),  N'Completed'),
    (106, 5, N'Debit',  450.00, N'Business Insurance Premium',     N'Insurance',     DATEADD(DAY, -16, @now), N'Completed'),
    (107, 5, N'Debit',  175.00, N'Workers Comp Insurance',         N'Insurance',     DATEADD(DAY, -16, @now), N'Completed'),
    (108, 5, N'Credit', 2500.00, N'Freelance Payment',             N'Payroll',       DATEADD(DAY, -19, @now), N'Completed'),
    (109, 5, N'Debit',   42.00, N'Starbucks Coffee - Team',        N'Dining',        DATEADD(DAY, -8, @now),  N'Completed'),
    (110, 5, N'Debit',  850.00, N'Contractor Payment',             N'Payroll',       DATEADD(DAY, -23, @now), N'Completed'),
    (111, 5, N'Debit',  120.00, N'Parking - Monthly Garage',       N'Transport',     DATEADD(DAY, -1, @now),  N'Completed'),
    (112, 5, N'Debit',  375.00, N'Equipment Purchase',             N'Subscription',  DATEADD(DAY, -26, @now), N'Pending');

SET IDENTITY_INSERT [Transactions] OFF;

-- =============================================================================
-- Transfers: 12 recent transfers across accounts
-- =============================================================================
SET IDENTITY_INSERT [Transfers] ON;

INSERT INTO [Transfers] ([Id], [FromAccountId], [ToAccountId], [Amount], [Status], [RequestedAt], [CompletedAt])
VALUES
    ( 1, 1, 3,   500.00, N'Completed',  DATEADD(DAY, -2, @now),  DATEADD(MINUTE, 1, DATEADD(DAY, -2, @now))),
    ( 2, 5, 1,  3000.00, N'Completed',  DATEADD(DAY, -5, @now),  DATEADD(MINUTE, 2, DATEADD(DAY, -5, @now))),
    ( 3, 2, 3,  1000.00, N'Completed',  DATEADD(DAY, -7, @now),  DATEADD(MINUTE, 1, DATEADD(DAY, -7, @now))),
    ( 4, 1, 2,   250.00, N'Completed',  DATEADD(DAY, -10, @now), DATEADD(MINUTE, 1, DATEADD(DAY, -10, @now))),
    ( 5, 3, 5,  5000.00, N'Completed',  DATEADD(DAY, -12, @now), DATEADD(MINUTE, 3, DATEADD(DAY, -12, @now))),
    ( 6, 1, 4,   750.00, N'Completed',  DATEADD(DAY, -15, @now), DATEADD(MINUTE, 1, DATEADD(DAY, -15, @now))),
    ( 7, 5, 2,  2500.00, N'Completed',  DATEADD(DAY, -18, @now), DATEADD(MINUTE, 2, DATEADD(DAY, -18, @now))),
    ( 8, 4, 3,   300.00, N'Completed',  DATEADD(DAY, -20, @now), DATEADD(MINUTE, 1, DATEADD(DAY, -20, @now))),
    ( 9, 5, 3, 10000.00, N'Completed',  DATEADD(DAY, -25, @now), DATEADD(MINUTE, 4, DATEADD(DAY, -25, @now))),
    (10, 1, 3,   200.00, N'Completed',  DATEADD(DAY, -3, @now),  DATEADD(MINUTE, 1, DATEADD(DAY, -3, @now))),
    (11, 2, 1,   100.00, N'Processing', DATEADD(MINUTE, -30, @now), NULL),
    (12, 1, 5,  1500.00, N'Failed',     DATEADD(DAY, -1, @now),  DATEADD(MINUTE, 5, DATEADD(DAY, -1, @now)));

SET IDENTITY_INSERT [Transfers] OFF;

PRINT 'Seed data inserted: 5 accounts, 112 transactions, 12 transfers.';
GO
