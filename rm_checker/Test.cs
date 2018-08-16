using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace rm_checker
{
	[TestFixture]
	public class Test
	{
		private IWebDriver driver;
		private WebDriverWait wait;

		[SetUp]
		public void Start()
		{
			ChromeOptions options = new ChromeOptions();
			if (bool.Parse(ConfigurationManager.AppSettings["headless_browser_mode"]))
			{
				options.AddArgument("headless");
			}
			driver = new ChromeDriver(options);
			wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
		}

		[Test]
		public void CheckRM()
		{
			var rmUser = new User
			{
				Login = ConfigurationManager.AppSettings["RM_user"],
				Password = ConfigurationManager.AppSettings["RM_password"]
			};

			var continErrorsName = "RM_errors_conti.txt";
			FileInfo continErrorsInfo = CreateFileInfoToAssemblyDirectory(continErrorsName);

			if (!continErrorsInfo.Exists)
			{
				using (var f = File.Create(continErrorsInfo.FullName)) { };
				File.WriteAllText(continErrorsInfo.FullName, "0");
			}

			try
			{
				LoginRM(rmUser);
				File.WriteAllText(continErrorsInfo.FullName, "0");
			}

			catch (Exception e)
			{
				int contiErrorsCount = int.Parse(File.ReadAllText(continErrorsInfo.FullName));
				contiErrorsCount++;

				if ( contiErrorsCount > int.Parse(ConfigurationManager.AppSettings["Continuous_Errors_Count"]) )
				{
					SendTelegramToRMAdmins($"Не удалось зайти в систему Redmine {contiErrorsCount} раз подряд!!! Свяжитесь с Корнеевым Дмитрием");
					contiErrorsCount = 0;
				}

				File.WriteAllText(continErrorsInfo.FullName, contiErrorsCount.ToString());
			}
		}

		private static FileInfo CreateFileInfoToAssemblyDirectory(string name)
		{
			return new FileInfo(Path.Combine(
					Path.GetDirectoryName(
						Assembly.GetExecutingAssembly().Location),
						name));
		}

		private void SendTelegramToRMAdmins(string message)
		{
			var telegramURL = @"https://api.telegram.org";
			var token = ConfigurationManager.AppSettings["RM_bot_token"];


			var chatIds = ConfigurationManager.AppSettings["Telegram_ChatIds"];
			var cIds = chatIds.Split(',');

			foreach (string chatId in cIds)
			{
				string url = $"{telegramURL}/bot{token}/sendMessage?chat_id={chatId}&text={message}";
				driver.Url = url;
			}
		}

		private void LoginRM(User user)
		{
			var login = user.Login;
			var pass = user.Password;

			if (login == "" || pass == "") { throw new ArgumentException("Please provide correct login data"); }

			driver.Url = "http://195.19.40.194:81/redmine/login";
			driver.FindElement(By.CssSelector("input#username")).SendKeys(Keys.Home + login);
			driver.FindElement(By.CssSelector("input#password")).SendKeys(Keys.Home + pass);

			var oldPage = driver.FindElement(By.CssSelector("form"));

			driver.FindElement(By.CssSelector(".us-log-in-btn")).Click();
			wait.Until(ExpectedConditions.StalenessOf(oldPage));
		}

		[TearDown]
		public void Stop()
		{
			driver.Quit();
			driver = null;
		}
	}
}