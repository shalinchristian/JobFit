-- Run this once to create the JobFit database and its two tables.

IF DB_ID('JobFitDB') IS NULL
    CREATE DATABASE JobFitDB;
GO

USE JobFitDB;
GO

IF OBJECT_ID('Users') IS NULL
CREATE TABLE Users
(
    UserID       INT IDENTITY(1,1) PRIMARY KEY,
    Username     NVARCHAR(50)  NOT NULL UNIQUE,
    Email        NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(200) NOT NULL,
    CreatedAt    DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

IF OBJECT_ID('Applications') IS NULL
CREATE TABLE Applications
(
    AppID       INT IDENTITY(1,1) PRIMARY KEY,
    UserID      INT           NOT NULL REFERENCES Users(UserID) ON DELETE CASCADE,
    Company     NVARCHAR(100) NOT NULL,
    Role        NVARCHAR(100) NOT NULL,
    Location    NVARCHAR(100) NULL,
    AppliedDate DATE          NOT NULL,
    Status      NVARCHAR(20)  NOT NULL
        CHECK (Status IN ('Applied', 'Interview', 'Offer', 'Rejected')),
    Notes       NVARCHAR(500) NULL
);
GO
