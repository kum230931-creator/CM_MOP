SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
USE CMOmeetsDB;
DELETE ur FROM AspNetUserRoles ur JOIN AspNetUsers u ON u.Id=ur.UserId WHERE u.UserName='logintest';
DELETE FROM AspNetUsers WHERE UserName='logintest';
DECLARE @id nvarchar(50)=CONVERT(nvarchar(50),NEWID());
DECLARE @rid nvarchar(50)=(SELECT TOP 1 Id FROM AspNetRoles WHERE Name='admin');
INSERT INTO AspNetUsers (Id,UserName,NormalizedUserName,Email,NormalizedEmail,EmailConfirmed,PasswordHash,SecurityStamp,ConcurrencyStamp,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnabled,AccessFailedCount,DisplayName,IsActive,CreatedAt)
VALUES (@id,N'logintest',N'LOGINTEST',N'logintest@cmomeets.local',N'LOGINTEST@CMOMEETS.LOCAL',1,N'LEG$3c7959e8355f19cb6c7a023e46099e5ea9ef23cc4c75675d153b366289fa1d1df18134229825b75064c6a4e86d97e3fa6ebaaed2c1da8c93500024c3c3f4ffd4',CONVERT(nvarchar(50),NEWID()),CONVERT(nvarchar(50),NEWID()),0,0,1,0,N'Login Test User',1,GETUTCDATE());
IF @rid IS NOT NULL INSERT INTO AspNetUserRoles(UserId,RoleId) VALUES(@id,@rid);
PRINT 'inserted logintest (admin)';
