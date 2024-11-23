INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
VALUES (1, 'Admin', 'ADMIN', NEWID());

INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
VALUES (2, 'Operator', 'OPERATOR', NEWID());

INSERT INTO [AspNetUsers] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount])
VALUES (1, 'Coditech', 'CODITECH', NULL, NULL, 0,HASHBYTES('SHA2_256', 'coditech'), NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0);
  
INSERT INTO [AspNetUsers] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount])
VALUES (2, 'Admin', 'Admin', NULL, NULL, 0,HASHBYTES('SHA2_256', 'admin'), NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0);
      
INSERT INTO [AspNetUsers] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount])
VALUES (3, 'User', 'USER', NULL, NULL, 0,HASHBYTES('SHA2_256', 'user'), NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0);

INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
VALUES (1, 1);

INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
VALUES (2, 1);

INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
VALUES (3, 2);

INSERT INTO Products (Description) VALUES 
('Template1'),
('Template2'),
('Template3');

INSERT INTO OperativeModes (Description) VALUES 
('Manual'), 
('CSE Protocol'), 
('Printer Language');

INSERT INTO DataTypes (Description) VALUES 
('Integer'), 
('Double'), 
('String'), 
('Boolean');

INSERT INTO PointTypes (Description) VALUES 
('Home'), 
('Pickup Point'),
('Clearance Point'),
('Application Point');

INSERT INTO RouteTypes (Description) VALUES 
('Pickup Route'), 
('Application Route');

INSERT INTO AppSettings (Description, Value, DataTypeId) VALUES 
('ActiveOperativeMode', '3', 1),
('ActiveProduct', '7', 1),
('DigitalIOSignalsEnabled', 'false', 4),
('LastExecutedRoute', '0', 1),
('LastExecutedRouteStep', '1', 1),
('ExecuteEntireCycleEnabled', 'false', 4),
('RobotOverride', '100', 1);

INSERT INTO Labels (ProductId, Content) VALUES 
(1, 'SIZE 41.2 mm, 23 mm
GAP 3 mm, 0 mm
SET RIBBON ON
DIRECTION 0,0
REFERENCE 0,0
CLS
DMATRIX 443,227,443,227,x6,r180,18,18,"www.pluriservice.it"
CODEPAGE 1252
ERASE 73,32,347,55
TEXT 419,86,"ROMAN.TTF",180,1,11,"PLURISERVICE.IT"
REVERSE 73,32,347,55
TEXT 300,231,"0",180,7,6,"05/27/2024"
TEXT 153,230,"0",180,7,6,"10:38:43"
TEXT 298,173,"0",180,8,8,"PLURISERVICE.IT"
BAR 56,120, 244, 2
BAR 53,194, 246, 2
PRINT 1,1
'),
(2, 'SIZE 41.2 mm, 23 mm
GAP 3 mm, 0 mm
SET RIBBON ON
DIRECTION 0,0
REFERENCE 0,0
CLS
DMATRIX 443,227,443,227,x6,r180,18,18,"www.pluriservice.it"
CODEPAGE 1252
ERASE 73,32,347,55
TEXT 419,86,"ROMAN.TTF",180,1,11,"PLURISERVICE.IT"
REVERSE 73,32,347,55
TEXT 300,231,"0",180,7,6,"05/27/2024"
TEXT 153,230,"0",180,7,6,"10:38:43"
TEXT 298,173,"0",180,8,8,"PLURISERVICE.IT"
BAR 56,120, 244, 2
BAR 53,194, 246, 2
PRINT 1,1
'),
(3, 'SIZE 41.2 mm, 23 mm
GAP 3 mm, 0 mm
SET RIBBON ON
DIRECTION 0,0
REFERENCE 0,0
CLS
DMATRIX 443,227,443,227,x6,r180,18,18,"www.pluriservice.it"
CODEPAGE 1252
ERASE 73,32,347,55
TEXT 419,86,"ROMAN.TTF",180,1,11,"PLURISERVICE.IT"
REVERSE 73,32,347,55
TEXT 300,231,"0",180,7,6,"05/27/2024"
TEXT 153,230,"0",180,7,6,"10:38:43"
TEXT 298,173,"0",180,8,8,"PLURISERVICE.IT"
BAR 56,120, 244, 2
BAR 53,194, 246, 2
PRINT 1,1
');

INSERT INTO RobotPoints (PointTypeId, X, Y, Z, Yaw, Pitch, Roll) VALUES 
(1, -353.864, 63.853,  -55.888, 79.433, -2.009, 91.334),
(2, -187.176, 70.122, -156.514, 85.138, -0.661, 91.129),
(3, -195.649, 63.854, -163.241, 79.433, -2.009, 91.334),
(1, -353.864, 63.853,  -55.888, 79.433, -2.009, 91.334),

(1, -353.865,   63.854, -55.889,   79.433, -2.009,  91.334),
(3, -627.075, -116.861, -90.803, -179.672, -1.358, 100.854),
(4, -739.709, -116.843, -90.774, -179.672, -1.358, 100.853),
(3, -627.075, -116.861, -90.803, -179.672, -1.358, 100.854),
(3, -353.865,   63.854, -55.889,   79.433, -2.009,  91.334),

(1, -353.865, 63.854,  -55.889, 79.433, -2.009, 91.334),
(2, -187.176, 70.121, -156.514, 85.138, -0.660, 91.128),
(3, -195.649, 63.854, -163.241, 79.433, -2.009, 91.334),
(1, -353.865, 63.854,  -55.889, 79.433, -2.009, 91.334),

(1, -382.139, 225.558, -71.927,  92.456, -1.476,   83.647),
(3, -460.935,  63.854, 108.810, -11.197, 88.428, -101.384),
(4, -520.820,  63.854, 108.811, -11.165, 88.428, -101.352),
(3, -460.935,  63.854, 108.810, -11.197, 88.428, -101.384),
(1, -382.139, 225.558, -71.927,  92.456, -1.476,   83.647),

(1, -353.864, 63.853,  -55.888, 79.433, -2.009, 91.334),
(2, -187.176, 70.122, -156.514, 85.138, -0.661, 91.129),
(3, -195.649, 63.854, -163.241, 79.433, -2.009, 91.334),
(1, -353.864, 63.853,  -55.888, 79.433, -2.009, 91.334),

(1, -353.865, 63.854, -55.889,  79.433, -2.009, 91.334),
(3, -353.865, 63.853, -55.889, 176.292, -1.085, 87.846),
(4, -389.264, 63.850, -55.890, 176.292, -1.083, 87.843),
(3, -353.865, 63.853, -55.889, 176.292, -1.085, 87.846),
(1, -353.865, 63.854, -55.889,  79.433, -2.009, 91.334);

INSERT INTO Routes (ProductId, RouteTypeId) VALUES
(1, 1),
(1, 2),
(2, 1),
(2, 2),
(3, 1),
(3, 2);

INSERT INTO RouteSteps (RouteId, RobotPointId, StepOrder, Speed, ClearZone) VALUES
(1, 1, 1, 50, 'false'),
(1, 2, 2, 50, 'false'),
(1, 3, 3, 50, 'false'),
(1, 4, 4, 50, 'false'),

(2, 5, 1, 50, 'false'),
(2, 6, 2, 50, 'false'),
(2, 7, 3, 50, 'false'),
(2, 8, 4, 50, 'false'),
(2, 9, 5, 50, 'false'),

(3, 10, 1, 50, 'false'),
(3, 11, 2, 50, 'false'),
(3, 12, 3, 50, 'false'),
(3, 13, 4, 50, 'false'),

(4, 14, 1, 50, 'false'),
(4, 15, 2, 50, 'false'),
(4, 16, 3, 50, 'false'),
(4, 17, 4, 50, 'false'),
(4, 18, 5, 50, 'false'),

(5, 19, 1, 50, 'false'),
(5, 20, 2, 50, 'false'),
(5, 21, 3, 50, 'false'),
(5, 22, 4, 50, 'false'),

(6, 23, 1, 50, 'false'),
(6, 24, 2, 50, 'false'),
(6, 25, 3, 50, 'false'),
(6, 26, 4, 50, 'false'),
(6, 27, 5, 50, 'false');