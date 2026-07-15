# Nerdle Telegram Bot

A Telegram bot that helps solve puzzles from **Nerdle** (https://nerdlegame.com).

The bot assists with finding valid equations, filtering possible solutions based on feedback from previous guesses, and calculating the best next move.

## About Nerdle

Nerdle is a daily mathematical puzzle similar to Wordle, but instead of words you need to guess a valid equation.

Play here:
https://nerdlegame.com

## Features

- 🔍 Validate whether an equation is syntactically correct.
- 🚫 Check why an equation is invalid.
- 🎯 Filter all possible solutions using the feedback from previous guesses.
- 📊 Calculate the best next guess to maximize information.

## Commands

### `/must`

Displays the current **required symbols** ("must" set) and waits for you to enter a new set. Use this command whenever you want to update the symbols that are known to be present in the solution.

**Example:**
```
/must

> Must: 12456+*=
```

---

### `/forbidden`

Displays the current **forbidden symbols** and waits for you to enter a new set. Use this command whenever you want to update the symbols that are known **not** to appear in the solution.


**Example:**
```
/forbidden

> Forbidden: 37890-/
```

---

### `/pattern`

Displays the current **RegEx pattern** used to filter possible solutions and waits for you to enter a new pattern. Use this command to update the pattern based on the information from your previous guesses.

**Example:**
```
/pattern

> Pattern: [^5][^=][^2][^6]4[^1][^+][^*]
```

---

### `/calculate`

Calculates the best next guess using the current game state. The suggested equation aims to eliminate as many remaining possibilities as possible.

**Example:**
```
/calculate

> Found 1 solution(s).
> 1+6*4=25
```

---

### `/cancel`

Cancels the current input operation and displays the current values of **must**, **forbidden**, and **pattern**. Use this command if you decide not to update a value and want to return to the current game state.

**Example:**
```
/cancel

> Must: 12456+*=
> Forbidden: 37890-/
> Pattern: [^5][^=][^2][^6]4[^1][^+][^*]
```

## Screenshot Recognition

Instead of entering the game state manually, you can simply send the bot a screenshot of your current Nerdle game.

The bot will automatically analyze the image and extract:

- **must** symbols;
- **forbidden** symbols;
- the **pattern** describing all possible solutions.

If enough information is available to uniquely determine the set of symbols (all digits and operators are either **must** or **forbidden**, allowing at most one unknown symbol), the bot will immediately start searching for the best solution without requiring any additional input.

## How it works

The bot keeps track of the feedback from your previous guesses and continuously narrows the set of possible solutions.

Using the remaining candidates, it can:

- determine required symbols;
- determine forbidden symbols;
- generate a matching regular expression;
- recommend the most informative next guess.

## 🚀 Installation

Download archive from the [**Releases**](https://github.com/varajan/NerdleSolverBot/releases) section.

---

## 🔧 Bot Setup

1. Open Telegram and search for **BotFather**

2. Start a chat and run:

   ```
   /start
   ```

3. Create a new bot:

   ```
   /newbot
   ```

4. Follow the instructions:

   * Enter a name for your bot
   * Enter a unique username (must end with `bot`)

5. After creation, you will receive a **Bot Token**

6. Open the existing file:

   ```
   BotID.txt
   ```

7. Paste your token into this file (replace its contents):

   ```
   123456789:ABCdefGhIJKlmNoPQRsTUVwxyZ
   ```

8. Save the file and make sure it remains in the same directory as the application executable

9. Simply run:

```
NerdleSolverBot.exe
```

If SmartScreen blocks the app:

1. Click **More info**
2. Click **Run anyway**

---
