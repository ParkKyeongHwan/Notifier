namespace Notifier
{
    public class OcrCorrector
    {
        private static readonly List<string> IgnoredKeywords = new List<string>
        {
            // 1. 공통 포함 단어 (이 단어가 들어가면 무조건 스킵)
            "어설트",
            "패러사이트",
            "침공",

            // 2. 고유 어설트 명칭
            "거미의숲",
            "시공",
            "바벨탑",
            "불시착",
            "부산행",
            "나로도",
            "우주센터",
            "우주",
            "마추픽추",
            "마추",
            "픽추",
            "모선",
            "유령선",
            "보르네오섬",
            "보르",
            "피라미드",
            "피라",
            "대마도",
            "명성황후",
            "명성",
            "마피아저택",
            "마피아",
            "하수도",
            "경성역",
            "시티",
            "시티타워",
            "한강둔치",
            "트랜실바니아",
            "트랜실",
            "정글",
            "달기지",
            "연구단지",
        };

        private static readonly Dictionary<string, string> MissionDictionary = new Dictionary<string, string>
        {
            // --- 1. 농업 및 외곽 구역 ---
            { "한계농지", "한계농지 논밭 습격전 (상위 3명)" },
            { "구역방벽", "농업진흥구역 방벽 습격전 (상위 5명)" },
            { "농장폐허", "농업보호구역 농장 폐허 습격전 (상위 15명)" }, // '서쪽산길 폐허'와 충돌 방지
            { "과수원중앙", "과수원 중앙 습격전 (상위 50명)" }, // 다른 '중앙'과 충돌 방지
            { "전면도로", "카페촌 전면도로 습격전 (상위 30명)" },
            { "경작지", "농업진흥구역 경작지 습격전 (상위 30명)" },
            { "팔당댐", "팔당댐 공원 습격전 (상위 20명)" },

            // --- 2. 기업 및 공업 지구 ---
            { "외국기업", "외국기업지구 하부 습격전 (상위 20명)" },
            { "상가단지", "상업지구 상가단지 습격전 (상위 150명)" },
            { "복합지구중앙", "복합지구 중앙 습격전 (상위 10명)" },
            { "복합지구상부", "복합지구 상부공장 습격전 (상위 5명)" },
            { "산란지", "경공업지구 산란지 습격전 (상위 100명)" },
            { "중공업상부", "중공업지구 상부공장 습격전 (상위 20명)" }, 

            // --- 3. 산책로 및 체험농장 ---
            { "산책로광장", "산책로 광장 습격전 (상위 3명)" },
            { "산책로중앙", "산책로 중앙 습격전 (상위 50명)" },
            { "두물경", "산책로 두물경 습격전 (상위 150명)" },
            { "산책로B", "산책로 B (상위 50명)" },
            { "굽은길", "산책로 굽은길 습격전 (상위 15명)" },
            { "체험농장하부", "체험농장 하부 습격전 (상위 30명)" },
            { "체험농장상부", "체험농장 상부 습격전 (상위 10명)" },

            // --- 4. 산길 및 계곡 ---
            { "개미굴", "계곡길 개미굴 습격전 (상위 100명)" },
            { "개미협곡", "계곡길 개미협곡 습격전 (상위 5명)" },
            { "깊은산길중턱", "깊은산길 중턱 습격전 (상위 50명)" },
            { "개미군락", "깊은산길 개미군락 습격전 (상위 3명)" },
            { "운길산초입", "운길산 초입 공터 습격전 (상위 100명)" },
            { "운길산", "운길산 초입 공터 습격전 (상위 100명)" },
            { "초입", "운길산 초입 공터 습격전 (상위 100명)" },
            { "공터", "운길산 초입 공터 습격전 (상위 100명)" },
            { "오솔길중앙", "오솔길 중앙 습격전 (상위 10명)" },
            { "아라크니아", "서쪽산길 아라크니아 습격전 (상위 150명)" },
            { "서쪽산길폐허", "서쪽산길 폐허 습격전 (상위 15명)" },
            { "상위", "알 수 없는 습격전" }
        };

        // 미션별 인원수 매핑 (인원수를 기준으로 후보를 필터링하기 위함)
        private static readonly Dictionary<string, int> MissionPeopleCount = new Dictionary<string, int>
        {
            // 3명
            { "한계농지 논밭 습격전 (상위 3명)", 3 },
            { "산책로 광장 습격전 (상위 3명)", 3 },
            { "깊은산길 개미군락 습격전 (상위 3명)", 3 },

            // 5명
            { "농업진흥구역 방벽 습격전 (상위 5명)", 5 },
            { "복합지구 상부공장 습격전 (상위 5명)", 5 },
            { "계곡길 개미협곡 습격전 (상위 5명)", 5 },

            // 10명
            { "복합지구 중앙 습격전 (상위 10명)", 10 },
            { "체험농장 상부 습격전 (상위 10명)", 10 },
            { "오솔길 중앙 습격전 (상위 10명)", 10 },

            // 15명
            { "농업보호구역 농장 폐허 습격전 (상위 15명)", 15 },
            { "산책로 굽은길 습격전 (상위 15명)", 15 },
            { "서쪽산길 폐허 습격전 (상위 15명)", 15 },

            // 20명
            { "팔당댐 공원 습격전 (상위 20명)", 20 },
            { "외국기업지구 하부 습격전 (상위 20명)", 20 },
            { "중공업지구 상부공장 습격전 (상위 20명)", 20 },

            // 30명
            { "카페촌 전면도로 습격전 (상위 30명)", 30 },
            { "농업진흥구역 경작지 습격전 (상위 30명)", 30 },
            { "체험농장 하부 습격전 (상위 30명)", 30 },

            // 50명
            { "과수원 중앙 습격전 (상위 50명)", 50 },
            { "산책로 중앙 습격전 (상위 50명)", 50 },
            { "산책로 B (상위 50명)", 50 },
            { "깊은산길 중턱 습격전 (상위 50명)", 50 },

            // 100명
            { "경공업지구 산란지 습격전 (상위 100명)", 100 },
            { "계곡길 개미굴 습격전 (상위 100명)", 100 },
            { "운길산 초입 공터 습격전 (상위 100명)", 100 },

            // 150명
            { "상업지구 상가단지 습격전 (상위 150명)", 150 },
            { "산책로 두물경 습격전 (상위 150명)", 150 },
            { "서쪽산길 아라크니아 습격전 (상위 150명)", 150 }
        };

        public static (string Name, string Category, string Reason) ExtractMissionNameEnhanced(string rawOcrText)
        {
            string cleanText = rawOcrText.Replace(" ", "").Replace("\n", "").Replace("\r", "");

            // 1. 숫자(인원수) 추출 시도
            int extractedCount = -1;
            // 정규식을 사용하거나 단순히 문자열 내 포함 여부 확인
            // 흔한 인원수: 3, 5, 10, 15, 20, 30, 50, 100, 150
            int[] validCounts = { 150, 100, 50, 30, 20, 15, 10, 5, 3 }; // 큰 수부터 확인 (예: 150이 50에 매칭되지 않게)

            // 숫자만 추출하기 위한 정규식
            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(cleanText, @"\d+");
            if (match.Success)
            {
                if (int.TryParse(match.Value, out int num))
                {
                    if (validCounts.Contains(num))
                    {
                        extractedCount = num;
                        Console.WriteLine($"[인원수 감지] {extractedCount}명");
                    }
                }
            }

            // 만약 정규식으로 정확히 안 찾아진다면, 숫자 문자열이 포함되어 있는지 확인
            if (extractedCount == -1)
            {
                foreach (int count in validCounts)
                {
                    if (cleanText.Contains(count.ToString()))
                    {
                        // 150 안에 50이 포함되어 있으므로 추가 확인 필요
                        if (count == 50 && cleanText.Contains("150")) continue;
                        if (count == 10 && cleanText.Contains("100")) continue;
                        if (count == 15 && cleanText.Contains("150")) continue;

                        extractedCount = count;
                        Console.WriteLine($"[인원수 감지 (텍스트포함)] {extractedCount}명");
                        break;
                    }
                }
            }

            // 인원수가 감지되었다면 해당 인원수에 맞는 미션들만 필터링해서 검사
            if (extractedCount != -1)
            {
                var candidateMissions = MissionPeopleCount.Where(kvp => kvp.Value == extractedCount).Select(kvp => kvp.Key).ToList();

                string bestMatchName = string.Empty;
                int bestErrorCount = int.MaxValue;

                // 필터링된 후보들 중 키워드와 매칭
                foreach (var candidateName in candidateMissions)
                {
                    // Dictionary에서 이 전체 이름을 가리키는 키워드들을 찾음
                    var keywords = MissionDictionary.Where(kvp => kvp.Value == candidateName).Select(kvp => kvp.Key).ToList();

                    foreach (string targetKeyword in keywords)
                    {
                        // 텍스트가 매우 깨진 상황을 가정하여, 오타 허용치를 대폭 늘림
                        int allowedErrors = targetKeyword.Length / 2; // 절반까지 오타 허용!

                        if (cleanText.Length < targetKeyword.Length)
                        {
                            // 글자가 잘린 경우: 잘린 길이만큼만 비교
                            int errorCount = ComputeLevenshteinDistance(cleanText, targetKeyword.Substring(0, cleanText.Length));
                            if (errorCount <= allowedErrors && errorCount < bestErrorCount)
                            {
                                bestErrorCount = errorCount;
                                bestMatchName = candidateName;
                            }
                        }
                        else
                        {
                            for (int i = 0; i <= cleanText.Length - targetKeyword.Length; i++)
                            {
                                string window = cleanText.Substring(i, targetKeyword.Length);
                                int errorCount = ComputeLevenshteinDistance(window, targetKeyword);

                                if (errorCount <= allowedErrors && errorCount < bestErrorCount)
                                {
                                    bestErrorCount = errorCount;
                                    bestMatchName = candidateName;
                                }
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(bestMatchName))
                {
                    return (bestMatchName, "Success", $"인원수({extractedCount}명) 기반 매칭 성공");
                }
                else
                {
                    // 인원수는 찾았는데 글자를 도저히 매칭할 수 없는 경우, 인원수만이라도 반환할지 선택
                    // 일단은 알 수 없는 습격전(상위 N명)으로 반환
                    return ($"알 수 없는 습격전 (상위 {extractedCount}명)", "Unknown", $"인원수({extractedCount}명)만 감지됨. 이름 매칭 실패");
                }
            }

            // 인원수를 못 찾았거나 후보 매칭에 실패하면 기존 로직 수행
            return ExtractMissionName(rawOcrText);
        }

        public static (string Name, string Category, string Reason) ExtractMissionName(string rawOcrText)
        {
            // 1. OCR의 불필요한 공백과 줄바꿈을 모두 제거
            string cleanText = rawOcrText.Replace(" ", "").Replace("\n", "").Replace("\r", "");

            // 제외 키워드(어설트) 검사
            string bestAssaultMatch = string.Empty;
            int bestAssaultLength = -1;

            foreach (var ignored in IgnoredKeywords)
            {
                if (cleanText.Length < ignored.Length) continue;

                for (int i = 0; i <= cleanText.Length - ignored.Length; i++)
                {
                    string window = cleanText.Substring(i, ignored.Length);

                    // 짧은 단어(2글자 이하)는 오타를 허용하지 않음 (false positive 방지)
                    int allowedErrors = 0;
                    if (ignored.Length >= 5) allowedErrors = 2;
                    else if (ignored.Length >= 3) allowedErrors = 1;

                    if (ComputeLevenshteinDistance(window, ignored) <= allowedErrors)
                    {
                        // "어설트" 같은 공통 단어보다 길고 구체적인 단어(예: "경성역", "피라미드")를 우선함
                        if (ignored.Length > bestAssaultLength ||
                            (ignored.Length == bestAssaultLength && ignored != "어설트" && ignored != "침공"))
                        {
                            bestAssaultMatch = ignored;
                            bestAssaultLength = ignored.Length;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(bestAssaultMatch))
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 어설트 감지 ('{bestAssaultMatch}').");
                return ($"어설트 ({bestAssaultMatch})", "Assault", "예외 키워드(어설트) 감지");
            }

            string bestMatchName = string.Empty;
            int bestErrorCount = int.MaxValue; // 가장 적은 오타 수 기록

            // 2. 키워드 사전을 순회하며 검사
            foreach (var kvp in MissionDictionary)
            {
                string targetKeyword = kvp.Key;
                string fullName = kvp.Value;

                // 글자가 키워드보다 짧게 인식되었으면 패스
                if (cleanText.Length < targetKeyword.Length) continue;

                // 슬라이딩 윈도우 방식으로 키워드 길이만큼 잘라서 검사
                for (int i = 0; i <= cleanText.Length - targetKeyword.Length; i++)
                {
                    string window = cleanText.Substring(i, targetKeyword.Length);

                    // 두 단어의 차이(오타 개수)를 계산
                    int errorCount = ComputeLevenshteinDistance(window, targetKeyword);

                    // ★ 동적 오타 허용치 적용
                    // 키워드가 2글자 이하면 오타 허용 안함, 3~4글자면 오타 1개 허용, 5글자 이상이면 2개 허용
                    int allowedErrors = 0;
                    if (targetKeyword.Length >= 5) allowedErrors = 2;
                    else if (targetKeyword.Length >= 3) allowedErrors = 1;

                    if (errorCount <= allowedErrors)
                    {
                        // 완벽히 일치(오타 0)하면 무조건 정답이므로 즉시 반환!
                        if (errorCount == 0)
                        {
                            return (fullName, "Success", "완벽 매칭");
                        }

                        if (errorCount < bestErrorCount)
                        {
                            bestErrorCount = errorCount;
                            bestMatchName = fullName;
                        }
                    }
                }
            }

            // 가장 정확도 높은 결과를 찾았다면 반환(예: 1~2글자 오타로 통과한 최고 점수)
            if (!string.IsNullOrEmpty(bestMatchName))
            {
                if (bestMatchName == "알 수 없는 습격전")
                {
                    return (bestMatchName, "Unknown", $"오타 허용 범위({bestErrorCount}개) 내 알 수 없는 습격전 매칭");
                }
                return (bestMatchName, "Success", $"오타 허용 범위({bestErrorCount}개) 내 매칭");
            }

            // 구체적인 미션을 못 찾았을 경우 "습격전" 이라는 단어가 있는지 확인
            string fallbackKeyword = "습격";
            if (cleanText.Length >= fallbackKeyword.Length)
            {
                for (int i = 0; i <= cleanText.Length - fallbackKeyword.Length; i++)
                {
                    string window = cleanText.Substring(i, fallbackKeyword.Length);
                    // "습격전"(3글자)이므로 오타 1개까지는 허용하여 융통성 있게 검사
                    if (ComputeLevenshteinDistance(window, fallbackKeyword) <= 1)
                    {
                        return ("알 수 없는 습격전", "Unknown", "'습격' 키워드 단독 감지");
                    }
                }
            }

            // 최후의 수단: OCR이 심하게 깨져서 "습" 또는 "격" 단 한 글자만 인식된 경우
            if (cleanText.Contains("습") || cleanText.Contains("격"))
            {
                return ("알 수 없는 습격전", "Unknown", "'습' 또는 '격' 1글자 감지");
            }

            return ("미발견", "None", "매칭되는 단어 없음");
        }

        private static int ComputeLevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }
    }
}
