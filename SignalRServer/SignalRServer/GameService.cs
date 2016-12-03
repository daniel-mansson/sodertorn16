using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rovio
{
	public class GameService : IGameService
	{
		private List<Game> m_games = new List<Game>();
		private List<GameUser> m_users = new List<GameUser>();
		private Dictionary<string, Invitation> m_invitations = new Dictionary<string, Invitation>();
		private object m_userLock = new object();
		private object m_gamesLock = new object();
		private object m_invitationsLock = new object();
		private object m_idLock = new object();
		private int m_id;

		public IList<GameUser> Users
		{
			get { return m_users.ToList(); }
		}

		public IList<Game> Games
		{
			get { return m_games.ToList(); }
		}

		public GameUser SignIn(string name, string connectionId)
		{
			var gameUser = new GameUser()
			{
				Name = name,
				ConnectionId = connectionId,
				Status = GameUserStatus.NotInvitedYet
			};

			lock (m_userLock)
			{
				m_users.Add(gameUser);
			}

			return gameUser;
		}

		public Invitation CreateInvitation(string inviterId, IEnumerable<string> connectionIdsToInvite)
		{
			var invitationId = GetId().ToString();
			var inviter = GetUserById(inviterId);
			var usersToInvite = GetUsersByIds(connectionIdsToInvite);
			var invitation = new Invitation(invitationId, inviter, usersToInvite);

			lock (m_invitationsLock)
			{
				foreach (var id in connectionIdsToInvite)
				{
					var user = GetUserById(id);
					if (user != null)
					{
						user.Status = GameUserStatus.Invited;
					}
				}

				m_invitations.Add(invitation.Id, invitation);
				inviter.Status = GameUserStatus.Joined;
			}

			return invitation;
		}

		public bool RemoveInvitation(string invitationId)
		{
			lock (m_invitationsLock)
			{
				return m_invitations.Remove(invitationId);
			}
		}

		public Invitation GetInvitation(string invitationId)
		{
			Invitation invitation;
			m_invitations.TryGetValue(invitationId, out invitation);

			return invitation;
		}

		public bool AllUsersHaveDeclined(string invitationId)
		{
			return false;
		}

		public IList<Invitation> GetInvitationsForUser(string connectionId)
		{
			var user = GetUserById(connectionId);
			var query = from invitation in m_invitations.Values
						where invitation.InvitedUsers.Contains(user)
						select invitation;

			return query.ToList();
		}

		public IList<Invitation> GetMyInvitations(string connectionId)
		{
			var user = GetUserById(connectionId);
			var query = from invitation in m_invitations.Values
						where invitation.Inviter == user
						select invitation;

			return query.ToList();
		}

		public IList<Game> GetMyGames(string connectionId)
		{
			var user = GetUserById(connectionId);
			return m_games.Where(g => g.Users.Contains(user)).ToList();
		}

		public Game CreateGame(Invitation invitation)
		{
			var gameId = GetId().ToString();
			var game = new Game(gameId, invitation.AllUsers);

			lock (m_gamesLock)
			{
				m_games.Add(game);
			}

			return game;
		}

		public bool RemoveGame(string gameId)
		{
			var game = m_games.Find(g => g.Id == gameId);
			lock (m_invitationsLock)
			{
				return m_games.Remove(game);
			}
		}

		public IEnumerable<GameUser> GetUsersByIds(IEnumerable<string> ids)
		{
			foreach (var id in ids)
			{
				yield return GetUserById(id);
			}
		}

		public GameUser GetUserById(string connectionId)
		{
			return m_users.FirstOrDefault(u => u.ConnectionId == connectionId);
		}

		public GameUser GetUserByName(string name)
		{
			return m_users.FirstOrDefault(u => u.Name == name);
		}

		public void SignOut(string connectionId)
		{
			var user = GetUserById(connectionId);
			if (user != null)
			{
				lock (m_userLock)
				{
					m_users.Remove(user);
				}
			}
		}

		private int GetId()
		{
			lock (m_idLock)
			{
				int id = m_id++;

				return id;
			}
		}
	}
}
