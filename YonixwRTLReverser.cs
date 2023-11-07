using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class YonixwRTLReverser
{
	// "22:22", "01/02/23", "1,000.55", "1,000", "44.55",
	public static Regex numberWords = new Regex("^\\d[/\\d:\\.\\,\\\\]*\\d+$",RegexOptions.Compiled);

	public static string processWord(string word)
	{
		if (numberWords.Match(word).Success)
		{
			return word;
		}

		char[] _txt = word.ToCharArray();
		Array.Reverse(_txt);
		string result = new string(_txt);
		return result;
	}

	public static bool isTag(string part)
	{
		if (string.IsNullOrEmpty(part)) return false;
		return part.IndexOf("<") > -1;
	}

	public static bool isCloseTag(string tag)
	{
		if (string.IsNullOrEmpty(tag)) return false;
		// Becuse No self closing tag like <br />
		// only standalone or a pair with closing:
		return tag.IndexOf("/") > -1;
	}

	static string[] tagSplit = new[] { "<", ">", " ", "=", "/" };
	public static bool sameTagPair(string open, string close)
	{
		// TODO: alternative of Regex just finding first group of "^[^a-zA-Z](a-zA-Z)"
		string openTag = open.Split(tagSplit, StringSplitOptions.RemoveEmptyEntries)[0];
		string closeTag = close.Split(tagSplit, StringSplitOptions.RemoveEmptyEntries)[0];
		Console.WriteLine(openTag + " =? " + closeTag);
		return openTag == closeTag;
	}

	// tag, word or spaces. order inside regex is important
	public static Regex txtPart = new Regex(@"(((\<[^\>]+\>)|[^\s\>\<]+|\s+))",
					RegexOptions.Compiled);

	public static List<string> getTextParts(string fulltxt)
    {
		MatchCollection matchs = txtPart.Matches(fulltxt);
		List<string> result = new List<string>();
		foreach(Match m in matchs)
        {
			result.Add(m.Value);
        }
		return result;
    }


	class lastTag
    {
		public string CloseTag;
		public string Before = "";
    }

	public static string reverseAllParts(List<string> parts)
    {
		string currentResult = "";
		lastTag currentTag = new lastTag() { CloseTag = "", Before = "" };

		Stack<lastTag> tagsWeSaw = new Stack<lastTag>();

		parts.Reverse();

        for (int i = 0; i < parts.Count; i++)
        {
			string p = parts[i];
			if (isTag(p))
            {
				if (!isCloseTag(p))
                {
					if (currentTag.CloseTag != "" && sameTagPair(p, currentTag.CloseTag))
                    {
						if (tagsWeSaw.Count == 0)
                        {
							Console.WriteLine("Error Reverse! return original!"
								+ " i=" + i + "/" + parts.Count 
								+ " open=" + p 
								+ " close=" + currentTag.CloseTag
								+ " before=" + currentTag.Before);
							parts.Reverse();
							return string.Join("", parts.ToArray());
                        }

						currentResult = 
							currentTag.Before +
							p + currentResult + currentTag.CloseTag;
						currentTag = tagsWeSaw.Pop();
                    }
					else
                    {
						// Open without close we found?
						// must be standalone tag Unity support
						// So just re add it
						currentResult += p;
                    }
                } else {
					tagsWeSaw.Push(currentTag);
					currentTag = new lastTag() { CloseTag = p, Before = currentResult };
					currentResult = "";
                }
            }
			else
            {
				// Reverse the word:
				currentResult += processWord(p);
            }
        }

		return currentResult;
    }

	public static string RTLFix(string raw)
    {
		return reverseAllParts(getTextParts(raw));
	}

	public static void Main(string[] args)
	{
		Console.WriteLine("Hello Mono World --------");

		int i = 0;

		//i = 1;
		//foreach (string[] t in sameTagTests)
		//{
		//	Console.WriteLine(sameTagPair(t[0], t[1]) + 
		//			"[" + i + "] '" + t[0] + "' =? '" + t[1] + "'");
		//	i++;
		//}

		i = 1;
        foreach(string t in tests)
        {
			Console.WriteLine("[" + i + "] '" + t + "'");
			Console.WriteLine(RTLFix(t));
			i++;
		}

		Console.ReadKey();
	}


	public static string[] tests = new[]
	{
		"a b c",
		"a <b> c",
		"<a>123 456</a>",
		"<a>123 456</a><b>777 888</b>",
		"<a>123 456</a><c=2><b>777 888</b>",
		"<a x=1>123 456<a x=2>777 888</a></a>",
		"<a x=1><b=4>123</b> 456<a x=2>777 888</a></a>",
		"<a x=1><b=4>123<c></b> 456<d><b><a x=2>777 888<e></a></b><f></a><g>",
		"<color=#68c0f0> (Bet Mishpat Mehozi </color>",
		"<color=#68c0f0> It poinst to </color><color=#ff0000>you...)</color>",
		"<color=#68c0f0> I want 1.23 dollar </color>",
	};

	public static string[][] sameTagTests = new string[][]
	{
		new [] {"<a>", "</a>"},
		new [] {"< a >", "< / a>"},
		new [] {"<a color=1>", "</a>"},
		new [] {"<a=2>", "</a>"},

		new [] {"<a>", "</aa>"},
		new [] {"< a >", "< / aa>"},
		new [] {"<a color=1>", "</aa>"},
		new [] {"<a=2>", "</aa>"},

		new [] {"<aa>", "</a>"},
		new [] {"< aa >", "< / a>"},
		new [] {"<aa color=1>", "</a>"},
		new [] {"<aa=2>", "</a>"},

		new [] {"<a>", "</b>"},
		new [] {"< a >", "< / b>"},
		new [] {"<a color=1>", "</b>"},
		new [] {"<a=2>", "</b>"},
	};

}



