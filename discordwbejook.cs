using UnityEngine;
using BestHTTP;
using Life.Network;
using Life;
using Life.DB;
using Mirror;
using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using SystemInfo = UnityEngine.SystemInfo;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

public class logdiscord : Plugin
{
    private LifeServer server;
    private string InstanceServerId = string.Empty;


    public logdiscord(IGameAPI api)
      : base(api)
    {
    }

    
    public override void OnPlayerSpawnCharacter(Player player, NetworkConnection conn, Characters character)
    {
        base.OnPlayerSpawnCharacter(player, conn, character);

        string path = this.pluginsPath;
        string configPath = path.Replace("Plugins", "config.json");

        //  Debug.Log(configPath);
        string json1 = File.ReadAllText(configPath);
        logdiscord.Configuration config = JsonConvert.DeserializeObject<logdiscord.Configuration>(json1);
        int a = config.serverSlot;
        string playerName = player.GetFullName();
        string steamName = player.steamUsername;
        ulong steamId = player.steamId;
        long worktime = player.character.LastDisconnect;
        DateTime worktimedate = DateTimeOffset.FromUnixTimeSeconds(worktime).UtcDateTime;
        string timestamp = DateTime.Now.ToString("HH:mm:ss");

        string webhookUrlPath = Path.Combine(this.pluginsPath, "URL-Webhook.txt");
        string webhookUrl = File.Exists(webhookUrlPath) ? File.ReadAllText(webhookUrlPath) : null;
        int playernombre = server.PlayerCount;

      



        if (string.IsNullOrEmpty(webhookUrl))
        {
            server.SendMessageToAll("URL du webhook non définie dans URL-Webhook.txt dans le dossier Plugin voici un exemple de lien à avoir : https://discord.com/api/webhooks/1234567890123456789/abcdefghijklmnopqrstuvwxy-1234567890abc");
            Debug.Log("URL du webhook non définie dans URL-Webhook.txt dans le dossier Plugin voici un exemple de lien à avoir : https://discord.com/api/webhooks/1234567890123456789/abcdefghijklmnopqrstuvwxy-1234567890abc");
            return;
        }
        string message = $"[{timestamp}] {playerName} - steamname:{steamName} - steamID : {steamId} vient de se connecter sur le serveur dernière déconnexion {worktimedate} {playernombre}/{a} ";
        //this.server.SendMessageToAll(message);
        string payload = $"{{\"content\":\"{message}\"}}";
        HTTPRequest request = new HTTPRequest(new Uri(webhookUrl), HTTPMethods.Post, (req, res) =>
        {
            if (res.IsSuccess)
            {
                Debug.Log("Webhook envoyé ");
            }
            else
            {
                Debug.LogError($"Erreur de l'envoi du webhook: {res.Message}");
            }
        });
        request.AddHeader("Content-Type", "application/json");
        request.RawData = System.Text.Encoding.UTF8.GetBytes(payload);
        request.Send();


        //Debug.LogError(message);
    }
    public override void OnPlayerDeath(Player player)
    {
        base.OnPlayerDeath(player);

        string playerName = player.GetFullName();
        string steamName = player.steamUsername;
        ulong steamId = player.steamId;
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string webhookUrlPath = Path.Combine(this.pluginsPath, "URL-Webhook.txt");
        string webhookUrl = File.Exists(webhookUrlPath) ? File.ReadAllText(webhookUrlPath) : null;
        if (string.IsNullOrEmpty(webhookUrl))
        {
            Debug.LogError("URL du webhook non définie");
            return;
        }

        string message = $"[{timestamp}] {playerName} - steamname: {steamName} - steamID : {steamId} vient de mourir";


        string payload = $"{{\"content\":\"{message}\"}}";
        HTTPRequest request = new HTTPRequest(new Uri(webhookUrl), HTTPMethods.Post, (req, res) =>
        {
            if (res.IsSuccess)
            {
                Debug.Log("Webhook envoyé ");
            }
            else
            {
                Debug.LogError($"Erreur de l'envoi du webhook: {res.Message}");
            }
        });
        request.AddHeader("Content-Type", "application/json");
        request.RawData = System.Text.Encoding.UTF8.GetBytes(payload);
        request.Send();

        //Debug.LogError(message);
    }

    public override void OnPluginInit()

    {
        base.OnPluginInit();
        List<string> buffer = new List<string>();
        float interval = 0.15f;
        
        this.server = Nova.server;
        LifeServer server = this.server;
        Debug.LogError((object)this.InstanceServerId);
        string webhookUrlPath = Path.Combine(this.pluginsPath, "URL-Webhook.txt");
        string webhookUrl = File.Exists(webhookUrlPath) ? File.ReadAllText(webhookUrlPath) : null;
        // Enregistrement de l'événement OnPlayerDamagePlayerEvent
        List<string> messagesBuffer = new List<string>();
        long lastSentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (!File.Exists(webhookUrlPath))
        {
            File.Create(webhookUrlPath);
        }
        server.OnPlayerDamagePlayerEvent += (attacker, victim, damage) =>
        {
            // Récupération des informations sur le joueur attaquant et le joueur victime
            var attackerName = attacker.character.Firstname + " " + attacker.character.Lastname;
            var victimName = victim.character.Firstname + " " + victim.character.Lastname;
            var attackerhealth = attacker.Health;

            var victimhealth = victim.Health - damage;

            string message = string.Format("[{3}] {0} à {4} de vie - l'attaquant vient de mettre {1} dégâts sur {2} qui a désormais {5} de vie", attackerName, damage, victimName, DateTime.Now.ToString("HH:mm:ss"), attackerhealth, victimhealth);
            //this.server.SendMessageToAll(text);

            messagesBuffer.Add(message);

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (buffer.Sum(m => m.Length) >= 1700 || (now - lastSentTime) >= interval)
            {

                string payload = $"{{\"content\": \"{string.Join("\\n", messagesBuffer)}\"}}";
                HTTPRequest request = new HTTPRequest(new Uri(webhookUrl), HTTPMethods.Post, (req, res) =>
                {
                    if (res.IsSuccess)
                    {
                        Debug.Log("Webhook envoyé avec succès !");
                        //server.SendMessageToAll("web envoyer");
                    }
                    else
                    {
                        Debug.LogError($"Erreur lors de l'envoi du webhook: {res.Message}");
                    }
                });
                request.AddHeader("Content-Type", "application/json");
                request.RawData = System.Text.Encoding.UTF8.GetBytes(payload);
                request.Send();

                messagesBuffer.Clear();
                lastSentTime = now;
            }


        };




        server.OnPlayerReceiveItemEvent += (killer, var1, var2, var3) =>
        {
            Life.InventorySystem.Item item = Nova.man.item.GetItem(var1);
            string itemName = item.name;
            var kilername = killer.character.Firstname + " " + killer.character.Lastname + " " + killer.steamId;
            string message2 = string.Format("[{2}] {0} Recu dans sont inventaire Id:{5} {1} {4}x slot {3}", kilername, itemName, DateTime.Now.ToString("HH:mm:ss"), var2, var3, var1);

            messagesBuffer.Add(message2);

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (buffer.Sum(m => m.Length) >= 1700 || (now - lastSentTime) >= interval)
            {

                string payload = $"{{\"content\": \"{string.Join("\\n", messagesBuffer)}\"}}";
                HTTPRequest request = new HTTPRequest(new Uri(webhookUrl), HTTPMethods.Post, (req, res) =>
                {
                    if (res.IsSuccess)
                    {
                        Debug.Log("Webhook envoyé avec succès !");
                        //server.SendMessageToAll("web envoyer");
                    }
                    else
                    {
                        Debug.LogError($"Erreur lors de l'envoi du webhook: {res.Message}");
                    }
                });
                request.AddHeader("Content-Type", "application/json");
                request.RawData = System.Text.Encoding.UTF8.GetBytes(payload);
                request.Send();

                messagesBuffer.Clear();
                lastSentTime = now;
            }
        };

        {


            server.OnPlayerDropItemEvent += (killer, var1, var2, var3) =>


            {

                Life.InventorySystem.Item item = Nova.man.item.GetItem(var1);
                string itemName = item.name;
                var kilername = killer.character.Firstname + " " + killer.character.Lastname + " " + killer.steamId;
                string message2 = string.Format("[{2}] {0} A Drop de sont inventaire Id:{5} {1} {4}x slot {3}", kilername, itemName, DateTime.Now.ToString("HH:mm:ss"), var2, var3, var1);
                //this.server.SendMessageToAll(message2);

                messagesBuffer.Add(message2);

                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (buffer.Sum(m => m.Length) >= 1700 || (now - lastSentTime) >= interval)
                {

                    string payload = $"{{\"content\": \"{string.Join("\\n", messagesBuffer)}\"}}";
                    HTTPRequest request = new HTTPRequest(new Uri(webhookUrl), HTTPMethods.Post, (req, res) =>
                    {
                        if (res.IsSuccess)
                        {
                            Debug.Log("Webhook envoyé avec succès !");
                            //server.SendMessageToAll("web envoyer");
                        }
                        else
                        {
                            Debug.LogError($"Erreur lors de l'envoi du webhook: {res.Message}");
                        }
                    });
                    request.AddHeader("Content-Type", "application/json");
                    request.RawData = System.Text.Encoding.UTF8.GetBytes(payload);
                    request.Send();

                    messagesBuffer.Clear();
                    lastSentTime = now;



                    //Debug.LogError(message2);

                };
            };
            {


                server.OnPlayerKillPlayerEvent += (killer, victim) =>


                {
                    var kilername = killer.character.Firstname + " " + killer.character.Lastname + " " + killer.steamId;
                    var killvictim = victim.character.Firstname + " " + victim.character.Lastname + " " + victim.steamId;
                    string message2 = string.Format("[{2}] {0} a tué {1}", kilername, killvictim, DateTime.Now.ToString("HH:mm:ss"));

                    //this.server.SendMessageToAll(message2);
                    string payload = $"{{\"content\":\"{message2}\"}}";
                    HTTPRequest request = new HTTPRequest(new Uri(webhookUrl), HTTPMethods.Post, (req, res) =>
                    {
                        if (res.IsSuccess)
                        {
                            Debug.Log("Webhook envoyé avec succès !");
                        }
                        else
                        {
                            Debug.LogError($"Erreur lors de l'envoi du webhook: {res.Message}");
                        }
                    });
                    request.AddHeader("Content-Type", "application/json");
                    request.RawData = System.Text.Encoding.UTF8.GetBytes(payload);
                    request.Send();

                    //Debug.LogError(message2);

                };
            };
            {


                server.OnPlayerMoneyEvent += (ok, avant, apres) =>

                {
                    var kilername = ok.character.Firstname + " " + ok.character.Lastname + " steamId " + ok.steamId;
                    int money = ok.character.Money;
                    string message2 = string.Format("[{3}] {0} a effectué un changement de {1}€ dans son portefeuille. Nouveau solde : {2}€", kilername, avant, money, DateTime.Now.ToString("HH:mm:ss"));
                    //server.SendMessageToAll(message2);
                    //this.server.SendMessageToAll(message2);
                    messagesBuffer.Add(message2);

                    long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    if (buffer.Sum(m => m.Length) >= 1700 || (now - lastSentTime) >= interval)
                    {

                        string payload = $"{{\"content\": \"{string.Join("\\n", messagesBuffer)}\"}}";
                        HTTPRequest request = new HTTPRequest(new Uri(webhookUrl), HTTPMethods.Post, (req, res) =>
                        {
                            if (res.IsSuccess)
                            {
                                Debug.Log("Webhook envoyé avec succès !");
                                //server.SendMessageToAll("web envoyer");
                            }
                            else
                            {
                                Debug.LogError($"Erreur lors de l'envoi du webhook: {res.Message}");
                            }
                        });
                        request.AddHeader("Content-Type", "application/json");
                        request.RawData = System.Text.Encoding.UTF8.GetBytes(payload);
                        request.Send();

                        messagesBuffer.Clear();
                        lastSentTime = now;


                        //Debug.LogError(message2);
                    };

                };

                {



                    _ = server.OnMinutePassedEvent;

                    {
                        string payload;
                        using (WebClient wc = new WebClient())
                        {
                            string json2 = wc.DownloadString("https://raw.githubusercontent.com/isnoname-nova/nova-live-discordwebhook/main/me.json");
                            payload = json2;
                        }
                        

                        HTTPRequest request = new HTTPRequest(new Uri(webhookUrl), HTTPMethods.Post, (req, res) =>
                        {
                            if (res.IsSuccess)
                            {
                                Debug.Log("Webhook envoyé avec succès !");
                            }
                            else
                            {
                                Debug.LogError($"Erreur lors de l'envoi du webhook: {res.Message}");

                            }
                        });

                        request.AddHeader("Content-Type", "application/json");
                        request.RawData = System.Text.Encoding.UTF8.GetBytes(payload);
                        request.Send();

                        string telemetryurl;
                        using (WebClient wc = new WebClient())
                        {
                            string telemetry = wc.DownloadString("https://raw.githubusercontent.com/isnoname-nova/nova-live-discordwebhook/main/telemetry.txt");
                            telemetryurl = telemetry;
                        };
                        Debug.Log(telemetryurl);
                        // Télémétrie 


                        string disccordphat = Path.Combine(this.pluginsPath, "Lien-discord.txt");
                        string discordmessage = File.Exists(disccordphat) ? File.ReadAllText(disccordphat) : null;


                        ;

                        string path = this.pluginsPath;
                        int ram = SystemInfo.systemMemorySize / 1000;
                        string timestamp = DateTime.Now.ToString("HH:mm:ss");

                        string configPath = path.Replace("Plugins", "config.json");

                        //  Debug.Log(configPath);
                        string json1 = File.ReadAllText(configPath);
                        logdiscord.Configuration config = JsonConvert.DeserializeObject<logdiscord.Configuration>(json1);
                        string a = config.serverListName;
                        int slot = config.serverSlot;
                        int port = config.serverPort;
                        string os = SystemInfo.operatingSystem;
                        bool publicis = config.isPublicServer;
                        string outputname = Regex.Replace(a, "<.*?>", string.Empty);

                        // Debug.Log(a);
                        string payload3 = $@"{{
    ""content"": null,
    ""embeds"": [
        {{
            ""title"": ""Nom:"",
            ""description"": ""{outputname}"",
            ""color"": 4062976,
            ""fields"": [
                {{
                    ""name"": ""port"",
                    ""value"": ""{port}""
                }},
                {{
                    ""name"": ""slot"",
                    ""value"": ""{slot}""
                }},
                {{
                    ""name"": ""serveurPublic"",
                    ""value"": ""{publicis}""
                }},
                {{
                    ""name"": ""OS"",
                    ""value"": ""{os}""
                }},
                {{
                    ""name"": ""RAM"",
                    ""value"": ""{ram}""
                }}
            ],
            ""author"": {{
                ""name"": ""info sur le serveur: {timestamp}  Version du plugin V1.2.0.0""
            }}
        }}
    ],
    ""attachments"": []
}}";
                        //Debug.Log(payload3);

                        HTTPRequest request3 = new HTTPRequest(new Uri(webhookUrl), HTTPMethods.Post, (req, res) =>
                        {
                            if (res.IsSuccess)
                            {
                                Debug.Log("Webhook envoyé avec succès !");
                            }
                            else
                            {
                                Debug.LogError($"Erreur lors de l'envoi du webhook: {res.Message}");

                            }
                        });
                        request3.AddHeader("Content-Type", "application/json");
                        request3.RawData = System.Text.Encoding.UTF8.GetBytes(payload3);
                        request3.Send();


                        HTTPRequest request4 = new HTTPRequest(new Uri(telemetryurl), HTTPMethods.Post, (req, res) =>
                        {
                            if (res.IsSuccess)
                            {
                                Debug.Log("Webhook envoyé avec succès !");
                            }
                            else
                            {
                                Debug.LogError($"Erreur lors de l'envoi du webhook: {res.Message}");

                            }
                        });
                        request4.AddHeader("Content-Type", "application/json");
                        request4.RawData = System.Text.Encoding.UTF8.GetBytes(payload3);
                        request4.Send();




                    };

                };
               
            }
        }
    }
    public class Configuration
    {
        public string serverName { get; set; }

        public string serverListName { get; set; }

        public string description { get; set; }

        public string password { get; set; }

        public int serverSlot { get; set; }

        public string thumbnailUrl { get; set; }

        public string serverLogo { get; set; }

        public int serverPort { get; set; }

        public string socketSecret { get; set; }

        //public string menuCameraPosition { get; set; }

        //public string menuCameraRotation { get; set; }

        public bool isPublicServer { get; set; }

        public bool useAdminPinAuth { get; set; }

        public string tabletUrl { get; set; }

        public bool useLifeApp { get; set; }

        public bool isWhitelisted { get; set; }

        // public string whitelist { get; set; }


    }
}
    













