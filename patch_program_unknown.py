import re

with open("Program.cs", "r", encoding="utf-8-sig") as f:
    content = f.read()

# Program.cs contains logic where it saves images to targetFolder based on `category`.
# If category is "Unknown", it checks if match "상위 \d+" and sets extractedText.
# Then below: `if (category != "Assault")` it saves to `desktopPath, category`.
# So if category is "Unknown", it saves to `Desktop/Unknown`.
# The only issue is `OcrCorrector.ExtractMissionName(rawText)` determines the category.
# If `rawText` is "알 수 없는 습격전", OcrCorrector might set category to "Unknown" or we can force it.
# Let's check how OcrCorrector behaves when input is "알 수 없는 습격전".
# Actually, the logic in Program.cs already does:
# string category = extractionResult.Category;
# and it saves the file into `{category}` folder. If we return "알 수 없는 습격전" from Recognizer,
# OcrCorrector returns: (bestMatchName, "Unknown", "오타 허용 범위... 매칭") or something similar.
# Wait, let's verify OcrCorrector.cs!
