using Practice.Utils;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Practice.Manager.Server
{
	[System.Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }

    [System.Serializable]
    public class LoginResponse
    {
        public bool success;
        public string token;

		public override string ToString()
		{
            return $"Result : {success}";
		}
	}

	public class AuthManager
    {
        private string jwtToken = null;

        public async Task SendLoginRequest(string username, string password, Action onComplete)
        {
            using (var client = new HttpClient())
            {
                string url = $"http://{Constants.IP_ADDR}:{Constants.PORT_AUTH}/login";

                var json = $"{{\"username\":\"{username}\", \"password\":\"{password}\"}}";
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    HttpResponseMessage response = await client.PostAsync(url, content);
                    LoginResponse result = JsonUtility.FromJson<LoginResponse>(await response.Content.ReadAsStringAsync());
                    jwtToken = result.token;

					Debug.Log("Response: " + result.ToString());

                    onComplete?.Invoke();
				}
                catch(System.Exception ex)
                {
                    Debug.LogError("Post Request Error: " + ex.Message);
                }
            }
        }

        public async Task SendProfileRequest()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    string url = $"http://{Constants.IP_ADDR}:{Constants.PORT_AUTH}/profile";

                    // add jwt token
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

                    HttpResponseMessage response = await client.GetAsync(url);

                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.Unauthorized:
                            Debug.LogWarning("GET - Unauthorized");
                            break;
                        case HttpStatusCode.Forbidden:
                            Debug.LogWarning("GET - Forbidden");
                            break;
                        case HttpStatusCode.OK:
							string responseString = await response.Content.ReadAsStringAsync();
							Debug.Log("Get Response : " + responseString);
							break;
                    }
                }
                catch(HttpRequestException e)
                {
                    Debug.LogError("Get Request Error : " + e.Message);
                }
            }
        }
    }
}