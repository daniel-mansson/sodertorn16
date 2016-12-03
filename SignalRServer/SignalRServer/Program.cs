using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rovio
{
	class Program
	{
		static void Main(string[] args)
		{
			using (WebApp.Start<Startup>("http://*:8080/"))
			{
				Console.WriteLine("Server running at http://*:8080/");
				Console.ReadLine();
			}
		}
	}

	public class Startup
	{
		private IGameService m_gameService = new GameService();

		public void Configuration(IAppBuilder app)
		{
			GlobalHost.DependencyResolver.Register(
				typeof(GameHub),
				() => new GameHub(m_gameService));

			app.UseCors(CorsOptions.AllowAll);
			app.MapSignalR();
		}
	}

	public class SurvivalGameService
	{


		public void Hello(string name)
		{

		}
	}

	public class SurvivalGameHub : Hub
	{
		public SurvivalGameHub()
		{

		}

		public override Task OnConnected()
		{
			Console.WriteLine("New connection: " + Context.ConnectionId);
			return base.OnConnected();
		}

		public override Task OnDisconnected(bool stopCalled)
		{
			Console.WriteLine("Disconnected: " + Context.ConnectionId);
			return base.OnDisconnected(stopCalled);
		}

		public async void Hello(string name)
		{
			if (m_gameService.GetUserByName(name) != null)
			{
				Clients.Caller.OnError("User already signed in.");
				return;
			}

			Console.WriteLine("User " + name + " signed in " + Context.ConnectionId);

			var gameUser = m_gameService.SignIn(name, Context.ConnectionId);

			await Groups.Add(Context.ConnectionId, "Lobby");

			Clients.Group("Lobby").OnUserSignedIn(gameUser);
		}

	}

	public class GameHub : Hub
	{
		private IGameService m_gameService;

		public GameHub(IGameService gameService)
		{
			m_gameService = gameService;
		}

		public override Task OnConnected()
		{
			Console.WriteLine("New connection: " + Context.ConnectionId);

			return base.OnConnected();
		}

		public override Task OnDisconnected(bool stopCalled)
		{
			Console.WriteLine("Disconnected: " + Context.ConnectionId);

			var gameUser = m_gameService.GetUserById(Context.ConnectionId);

			if (gameUser != null)
			{
				Console.WriteLine($"User {gameUser.Name} signed out {gameUser.ConnectionId}");
				m_gameService.SignOut(gameUser.ConnectionId);

				Clients.Group("Lobby").OnUserSignedOut(gameUser);
			}

			return base.OnDisconnected(stopCalled);
		}

		public async void SignIn(string name)
		{
			if (m_gameService.GetUserByName(name) != null)
			{
				Clients.Caller.OnError("User already signed in.");
				return;
			}

			Console.WriteLine("User " + name + " signed in " + Context.ConnectionId);

			var gameUser = m_gameService.SignIn(name, Context.ConnectionId);

			await Groups.Add(Context.ConnectionId, "Lobby");

			Clients.Group("Lobby").OnUserSignedIn(gameUser);
		}

		public async void SignOut()
		{
			var gameUser = m_gameService.GetUserById(Context.ConnectionId);

			if (gameUser != null)
			{
				Console.WriteLine($"User {gameUser.Name} signed out {gameUser.ConnectionId}");

				foreach (var invitation in m_gameService.GetMyInvitations(gameUser.ConnectionId))
				{
					AbortInvitation(invitation);
				}

				foreach (var invitation in GetInvitations())
				{
					AbortInvitation(invitation);
				}

				await EndGame();

				m_gameService.SignOut(gameUser.ConnectionId);

				Clients.Group("Lobby").OnUserSignedOut(gameUser);

				await Groups.Remove(Context.ConnectionId, "Lobby");
			}
		}

		public void SendLobbyChatMessage(string message)
		{
			var fromUser = m_gameService.GetUserById(Context.ConnectionId);

			Clients.OthersInGroup("Lobby").OnLobbyChatMessage(fromUser, message);
		}

		public void SendGameChatMessage(string gameId, string message)
		{
			var fromUser = m_gameService.GetUserById(Context.ConnectionId);

			Clients.OthersInGroup(gameId).OnGameChatMessage(gameId, fromUser, message);
		}

		public void CreateGameWithUsernames(IList<string> userNamesToInvite, bool quickMatch)
		{
			Console.WriteLine("Create game from user names");

			var connectionIds = new List<string>();
			foreach (var name in userNamesToInvite)
			{
				var user = m_gameService.GetUserByName(name);
				if (user != null)
				{
					connectionIds.Add(user.ConnectionId);
				}
			}

			CreateGame(connectionIds, quickMatch);
		}

		public void CreateGame(IList<string> connectionIdsToInvite, bool quickMatch)
		{
			var invitation = m_gameService.CreateInvitation(Context.ConnectionId, connectionIdsToInvite);
			Clients.Clients(connectionIdsToInvite).OnInvitationReceived(invitation);
		}

		public IList<Invitation> GetInvitations()
		{
			return m_gameService.GetInvitationsForUser(Context.ConnectionId);
		}

		public IList<GameUser> GetUsers()
		{
			return m_gameService.Users;
		}

		public async Task AcceptInvitation(string invitationId)
		{
			var invitation = m_gameService.GetInvitation(invitationId);
			if (invitation != null)
			{
				invitation.Accept(Context.ConnectionId);
				var user = m_gameService.GetUserById(Context.ConnectionId);

				await Clients.Client(invitation.Inviter.ConnectionId).OnUserAcceptedInvitation(user);
			}

			if (invitation.AllInvitedUsersHaveJoined())
			{
				await StartGame(invitation);
			}
		}

		public async Task DeclineInvitation(string invitationId)
		{
			var invitation = m_gameService.GetInvitation(invitationId);
			if (invitation != null)
			{
				var user = m_gameService.GetUserById(Context.ConnectionId);
				invitation.Decline(Context.ConnectionId);

				await Clients.Group("Lobby").OnUserDeclinedInvitation(user, invitationId);
				// TODO: Should decline be broadcast to every user in invitation? Maybe
				//await Clients.Client(invitation.Inviter.ConnectionId).OnUserDeclinedInvitation(user, invitationId);
			}

			if (invitation.AllInvitedUsersHaveJoined())
			{
				await StartGame(invitation);
			}
			else if (invitation.InvitedUsers.Count <= 0)
			{
				AbortInvitation(invitation);
			}
		}

		public void AbortInvitation(string invitationId)
		{
			var invitation = m_gameService.GetInvitation(invitationId);
			var user = m_gameService.GetUserById(Context.ConnectionId);

			if (invitation != null)
			{
				AbortInvitation(invitation);
			}
		}

		private async Task StartGame(Invitation invitation)
		{
			Console.WriteLine("Starting game");

			var game = m_gameService.CreateGame(invitation);

			foreach (var user in invitation.AllUsers)
			{
				await Groups.Add(user.ConnectionId, game.Id);
			}

			// Invitation not needed anymore
			m_gameService.RemoveInvitation(invitation.Id);

			Clients.Group(game.Id).OnGameStarted(game);
		}

		private async Task EndGame()
		{
			Console.WriteLine("Ending game");

			foreach(var game in m_gameService.GetMyGames(Context.ConnectionId))
			{
				await Clients.Group(game.Id).OnGameEnded(game);

				foreach (var user in game.Users)
				{
					await Groups.Remove(user.ConnectionId, game.Id);
				}
				m_gameService.RemoveGame(game.Id);
			}
		}

		public void SendGameMessage(string jsonBlob)
		{
			var  game = m_gameService.Games.FirstOrDefault(g => g.Users.Any(u => u.ConnectionId == Context.ConnectionId));

			if (game != null)
			{
				Console.WriteLine(string.Format("[Game {0}] {1}", game.Id, jsonBlob));
				Clients.Group(game.Id).OnGameMessage(jsonBlob);
			}
		}

		private void AbortInvitation(Invitation invitation)
		{
			Console.WriteLine("Invitation aborted " + invitation.Id);

			var connectionIds = from u in invitation.AllUsers
								select u.ConnectionId;

			m_gameService.RemoveInvitation(invitation.Id);

			Clients.Group("Lobby").OnInvitationAborted(invitation);
		}
	}
}
