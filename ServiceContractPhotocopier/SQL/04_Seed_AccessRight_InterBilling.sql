SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- A new menu item is invisible until somebody is granted the right to see it.
--
-- dbo.AccessRight is a GRANT table: a row means that user holds that right. Declaring the right in
-- PluginMain only makes it appear in the Access Rights screen; it gives it to nobody. Without this
-- seed, Inter-Billing Setup would ship hidden from every user including ADMIN, which reads as "the
-- feature is not there" rather than "you have not been given it" -- exactly the trap Billing Format
-- fell into.
--
-- Mirrored from Meter Type, the same as Billing Format: the nearest screen in the same General Setup
-- menu, and the one whose audience is the same people. Whoever may open Meter Type may open this;
-- whoever may not, still may not. Existing rows are left alone, so a deliberate revocation is not
-- undone on the next plugin load.

IF OBJECT_ID('dbo.AccessRight', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.AccessRight (CmdID, UserID)
    SELECT 'CMD_SHOW_SCP_SETUP_INTERBILL', a.UserID
      FROM dbo.AccessRight a
     WHERE a.CmdID = 'CMD_SHOW_SCP_SETUP_METER_TYPE'
       AND NOT EXISTS (SELECT 1 FROM dbo.AccessRight b
                        WHERE b.CmdID = 'CMD_SHOW_SCP_SETUP_INTERBILL' AND b.UserID = a.UserID);

    INSERT INTO dbo.AccessRight (CmdID, UserID)
    SELECT 'CMD_OPEN_SCP_SETUP_INTERBILL', a.UserID
      FROM dbo.AccessRight a
     WHERE a.CmdID = 'CMD_OPEN_SCP_SETUP_METER_TYPE'
       AND NOT EXISTS (SELECT 1 FROM dbo.AccessRight b
                        WHERE b.CmdID = 'CMD_OPEN_SCP_SETUP_INTERBILL' AND b.UserID = a.UserID);
END
GO
