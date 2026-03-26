CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;
CREATE TABLE "AspNetRoles" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetRoles" PRIMARY KEY,
    "Name" TEXT NULL,
    "NormalizedName" TEXT NULL,
    "ConcurrencyStamp" TEXT NULL
);

CREATE TABLE "AspNetUsers" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetUsers" PRIMARY KEY,
    "UserName" TEXT NULL,
    "NormalizedUserName" TEXT NULL,
    "Email" TEXT NULL,
    "NormalizedEmail" TEXT NULL,
    "EmailConfirmed" INTEGER NOT NULL,
    "PasswordHash" TEXT NULL,
    "SecurityStamp" TEXT NULL,
    "ConcurrencyStamp" TEXT NULL,
    "PhoneNumber" TEXT NULL,
    "PhoneNumberConfirmed" INTEGER NOT NULL,
    "TwoFactorEnabled" INTEGER NOT NULL,
    "LockoutEnd" TEXT NULL,
    "LockoutEnabled" INTEGER NOT NULL,
    "AccessFailedCount" INTEGER NOT NULL
);

CREATE TABLE "AspNetRoleClaims" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY AUTOINCREMENT,
    "RoleId" TEXT NOT NULL,
    "ClaimType" TEXT NULL,
    "ClaimValue" TEXT NULL,
    CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserClaims" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY AUTOINCREMENT,
    "UserId" TEXT NOT NULL,
    "ClaimType" TEXT NULL,
    "ClaimValue" TEXT NULL,
    CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserLogins" (
    "LoginProvider" TEXT NOT NULL,
    "ProviderKey" TEXT NOT NULL,
    "ProviderDisplayName" TEXT NULL,
    "UserId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
    CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserRoles" (
    "UserId" TEXT NOT NULL,
    "RoleId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserTokens" (
    "UserId" TEXT NOT NULL,
    "LoginProvider" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Value" TEXT NULL,
    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");

CREATE UNIQUE INDEX "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");

CREATE INDEX "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");

CREATE INDEX "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");

CREATE INDEX "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");

CREATE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");

CREATE UNIQUE INDEX "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('00000000000000_CreateIdentitySchema', '9.0.4');

ALTER TABLE "AspNetUsers" ADD "CreatedDate" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00';

ALTER TABLE "AspNetUsers" ADD "FirstName" TEXT NULL;

ALTER TABLE "AspNetUsers" ADD "LastName" TEXT NULL;

CREATE TABLE "Projects" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Projects" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "OwnerId" TEXT NOT NULL,
    "CreatedDate" TEXT NOT NULL,
    "ModifiedDate" TEXT NULL,
    "IsActive" INTEGER NOT NULL,
    CONSTRAINT "FK_Projects_AspNetUsers_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "ChargeCodes" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ChargeCodes" PRIMARY KEY AUTOINCREMENT,
    "Code" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "ProjectId" INTEGER NOT NULL,
    "ValidFromDate" TEXT NOT NULL,
    "ValidToDate" TEXT NOT NULL,
    "CreatedDate" TEXT NOT NULL,
    "ModifiedDate" TEXT NULL,
    "IsActive" INTEGER NOT NULL,
    CONSTRAINT "FK_ChargeCodes_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ProjectEngineers" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ProjectEngineers" PRIMARY KEY AUTOINCREMENT,
    "ProjectId" INTEGER NOT NULL,
    "UserId" TEXT NOT NULL,
    "AssignedDate" TEXT NOT NULL,
    "UnassignedDate" TEXT NULL,
    "IsActive" INTEGER NOT NULL,
    CONSTRAINT "FK_ProjectEngineers_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ProjectEngineers_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ProjectManagers" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ProjectManagers" PRIMARY KEY AUTOINCREMENT,
    "ProjectId" INTEGER NOT NULL,
    "UserId" TEXT NOT NULL,
    "AssignedDate" TEXT NOT NULL,
    "UnassignedDate" TEXT NULL,
    "IsActive" INTEGER NOT NULL,
    CONSTRAINT "FK_ProjectManagers_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ProjectManagers_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ProjectTasks" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ProjectTasks" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "ProjectId" INTEGER NOT NULL,
    "CreatedDate" TEXT NOT NULL,
    "ModifiedDate" TEXT NULL,
    "IsActive" INTEGER NOT NULL,
    CONSTRAINT "FK_ProjectTasks_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Activities" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Activities" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "TaskId" INTEGER NOT NULL,
    "UserId" TEXT NOT NULL,
    "DurationHours" decimal(5,2) NOT NULL,
    "ActivityDate" TEXT NOT NULL,
    "CreatedDate" TEXT NOT NULL,
    "ModifiedDate" TEXT NULL,
    "IsActive" INTEGER NOT NULL,
    CONSTRAINT "FK_Activities_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Activities_ProjectTasks_TaskId" FOREIGN KEY ("TaskId") REFERENCES "ProjectTasks" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ActivityChargeCodes" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ActivityChargeCodes" PRIMARY KEY AUTOINCREMENT,
    "ActivityId" INTEGER NOT NULL,
    "ChargeCodeId" INTEGER NOT NULL,
    "AllocatedHours" decimal(5,2) NOT NULL,
    "CreatedDate" TEXT NOT NULL,
    "ModifiedDate" TEXT NULL,
    CONSTRAINT "FK_ActivityChargeCodes_Activities_ActivityId" FOREIGN KEY ("ActivityId") REFERENCES "Activities" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ActivityChargeCodes_ChargeCodes_ChargeCodeId" FOREIGN KEY ("ChargeCodeId") REFERENCES "ChargeCodes" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_Activities_TaskId" ON "Activities" ("TaskId");

CREATE INDEX "IX_Activities_UserId" ON "Activities" ("UserId");

CREATE INDEX "IX_ActivityChargeCodes_ActivityId" ON "ActivityChargeCodes" ("ActivityId");

CREATE INDEX "IX_ActivityChargeCodes_ChargeCodeId" ON "ActivityChargeCodes" ("ChargeCodeId");

CREATE UNIQUE INDEX "IX_ChargeCodes_Code_ProjectId" ON "ChargeCodes" ("Code", "ProjectId");

CREATE INDEX "IX_ChargeCodes_ProjectId" ON "ChargeCodes" ("ProjectId");

CREATE UNIQUE INDEX "IX_ProjectEngineers_ProjectId_UserId" ON "ProjectEngineers" ("ProjectId", "UserId");

CREATE INDEX "IX_ProjectEngineers_UserId" ON "ProjectEngineers" ("UserId");

CREATE UNIQUE INDEX "IX_ProjectManagers_ProjectId_UserId" ON "ProjectManagers" ("ProjectId", "UserId");

CREATE INDEX "IX_ProjectManagers_UserId" ON "ProjectManagers" ("UserId");

CREATE INDEX "IX_Projects_OwnerId" ON "Projects" ("OwnerId");

CREATE INDEX "IX_ProjectTasks_ProjectId" ON "ProjectTasks" ("ProjectId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250719235734_InitialChronocodeModels', '9.0.4');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250720013158_AddRoles', '9.0.4');

CREATE TABLE "WorkAuthorizationArtifacts" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_WorkAuthorizationArtifacts" PRIMARY KEY AUTOINCREMENT,
    "ChargeCodeId" INTEGER NOT NULL,
    "FileName" TEXT NOT NULL,
    "OriginalFileName" TEXT NOT NULL,
    "ContentType" TEXT NOT NULL,
    "FileSizeBytes" INTEGER NOT NULL,
    "FilePath" TEXT NOT NULL,
    "Description" TEXT NULL,
    "UploadedByUserId" TEXT NOT NULL,
    "UploadedDate" TEXT NOT NULL,
    "ModifiedDate" TEXT NULL,
    "IsActive" INTEGER NOT NULL,
    CONSTRAINT "FK_WorkAuthorizationArtifacts_AspNetUsers_UploadedByUserId" FOREIGN KEY ("UploadedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_WorkAuthorizationArtifacts_ChargeCodes_ChargeCodeId" FOREIGN KEY ("ChargeCodeId") REFERENCES "ChargeCodes" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_WorkAuthorizationArtifacts_ChargeCodeId" ON "WorkAuthorizationArtifacts" ("ChargeCodeId");

CREATE INDEX "IX_WorkAuthorizationArtifacts_UploadedByUserId" ON "WorkAuthorizationArtifacts" ("UploadedByUserId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250720022324_AddWorkAuthorizationArtifacts', '9.0.4');

ALTER TABLE "ProjectTasks" ADD "IsComplete" INTEGER NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250726031844_AddIsCompleteToProjectTask', '9.0.4');

ALTER TABLE "Projects" ADD "IsComplete" INTEGER NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250726033104_AddIsCompleteToProject', '9.0.4');

COMMIT;

