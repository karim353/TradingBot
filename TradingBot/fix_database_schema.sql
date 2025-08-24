-- Скрипт для исправления схемы базы данных TradingBot
-- Делает все поля nullable (кроме Id, UserId, Date, PnL)

-- Включаем поддержку внешних ключей
PRAGMA foreign_keys = OFF;

-- Создаем временную таблицу с правильной схемой
CREATE TABLE "Trades_New" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "UserId" INTEGER NOT NULL,
    "Date" TEXT NOT NULL,
    "PnL" REAL NOT NULL,
    "NotionPageId" TEXT NULL,
    "Ticker" TEXT NULL,
    "Account" TEXT NULL,
    "Session" TEXT NULL,
    "Position" TEXT NULL,
    "Direction" TEXT NULL,
    "Context" TEXT NULL,
    "Setup" TEXT NULL,
    "Result" TEXT NULL,
    "RR" REAL NULL,
    "Risk" REAL NULL,
    "EntryDetails" TEXT NULL,
    "Comment" TEXT NULL,
    "Note" TEXT NULL,
    "Emotions" TEXT NULL
);

-- Копируем данные из старой таблицы в новую
INSERT INTO "Trades_New" (
    "Id", "UserId", "Date", "PnL", "NotionPageId", "Ticker", 
    "Account", "Session", "Position", "Direction", "Context", 
    "Setup", "Result", "RR", "Risk", "EntryDetails", 
    "Comment", "Note", "Emotions"
)
SELECT 
    "Id", "UserId", "Date", "PnL", "NotionPageId", "Ticker",
    "Account", "Session", "Position", "Direction", "Context",
    "Setup", "Result", "RR", "Risk", "EntryDetails",
    COALESCE("Comment", '') as "Comment",
    "Note", "Emotions"
FROM "Trades";

-- Удаляем старую таблицу
DROP TABLE "Trades";

-- Переименовываем новую таблицу
ALTER TABLE "Trades_New" RENAME TO "Trades";

-- Создаем индексы
CREATE INDEX "IX_Trades_UserId" ON "Trades" ("UserId");
CREATE INDEX "IX_Trades_Date" ON "Trades" ("Date");
CREATE INDEX "IX_Trades_UserId_Date" ON "Trades" ("UserId", "Date");

-- Включаем поддержку внешних ключей обратно
PRAGMA foreign_keys = ON;

-- Проверяем результат
SELECT sql FROM sqlite_master WHERE type='table' AND name='Trades';
