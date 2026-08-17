SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- A new menu item is invisible until somebody is granted the right to see it.
--
-- dbo.AccessRight is a GRANT table: a row means that user holds that right, and declaring the right
-- in PluginMain only makes it appear in the Access Rights screen, it does not give it to anyone. So
-- Billing Format shipped hidden from every user including ADMIN, which reads as "the feature is not
-- there" rather than "you have not been given it".
--
-- Rather than granting it to everyone, it is mirrored from Meter Type -- the closest existing screen
-- in the same General Setup menu, at the same sensitivity. Whoever may open Meter Type may open
-- Billing Format; whoever may not, still may not. Existing rows are left alone, so a deliberate
-- revocation is not undone on the next plugin load.

IF OBJECT_ID('dbo.AccessRight', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.AccessRight (CmdID, UserID)
    SELECT 'CMD_SHOW_SCP_SETUP_BILLING_FORMAT', a.UserID
      FROM dbo.AccessRight a
     WHERE a.CmdID = 'CMD_SHOW_SCP_SETUP_METER_TYPE'
       AND NOT EXISTS (SELECT 1 FROM dbo.AccessRight b
                        WHERE b.CmdID = 'CMD_SHOW_SCP_SETUP_BILLING_FORMAT' AND b.UserID = a.UserID);

    INSERT INTO dbo.AccessRight (CmdID, UserID)
    SELECT 'CMD_OPEN_SCP_SETUP_BILLING_FORMAT', a.UserID
      FROM dbo.AccessRight a
     WHERE a.CmdID = 'CMD_OPEN_SCP_SETUP_METER_TYPE'
       AND NOT EXISTS (SELECT 1 FROM dbo.AccessRight b
                        WHERE b.CmdID = 'CMD_OPEN_SCP_SETUP_BILLING_FORMAT' AND b.UserID = a.UserID);
END
GO
