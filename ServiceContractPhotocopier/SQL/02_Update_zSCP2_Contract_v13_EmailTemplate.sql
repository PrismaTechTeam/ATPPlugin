SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- v13: customer feedback 08/08 — a customer can have their OWN bulk-email wording (language, tone,
-- extra instructions) instead of everyone sharing one message. NULL (the default) = use whichever
-- template is marked IsDefault in the Email Template module, so nobody has to choose unless they
-- want to. Nullable on purpose: no FK, because deleting a template must not block a contract save —
-- an unresolvable key simply falls back to the default at send time.
IF COL_LENGTH('dbo.zSCP2_Contract', 'EmailTemplateKey') IS NULL
    ALTER TABLE dbo.zSCP2_Contract ADD EmailTemplateKey BIGINT NULL;
GO
