-- 1. Create Column Encryption Key (CEK)
CREATE COLUMN MASTER KEY CMK_ToDoApp
WITH (
    KEY_STORE_PROVIDER_NAME = 'CUSTOM_FILE_STORE',
    KEY_PATH = '/path/to/todoapp_cmk.pfx'
);

-- 2. Create Encrypted Column Master Key (ECMK)
CREATE COLUMN ENCRYPTION KEY CEK_ToDoApp
WITH VALUES (
    COLUMN_MASTER_KEY = 'CMK_ToDoApp',
    ALGORITHM = 'RSA_OAEP'
)

-- 3. Alter the Todos table - Add new encrypted column
ALTER TABLE dbo.Todos
    ADD DescriptionEncrypted NVARCHAR(MAX) COLLATE Latin1_General_BIN2 ENCRYPTED 
    WITH (
        ENCRYPTION_TYPE = RANDOMIZED,
        ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256',
        COLUMN_ENCRYPTION_KEY = CEK_ToDoApp
    ) NULL;

-- 4. Migrate existing data from Description to DescriptionEncrypted
UPDATE dbo.Todos
SET DescriptionEncrypted = Description;

-- 5. Drop the old unencrypted column (after verification)
-- ALTER TABLE dbo.Todos DROP COLUMN Description;

-- 6. Rename the encrypted column
-- sp_rename 'dbo.Todos.DescriptionEncrypted', 'Description', 'COLUMN';