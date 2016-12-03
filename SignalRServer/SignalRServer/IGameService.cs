using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rovio
{
	public enum GameUserStatus
	{
		NotInvitedYet,
		Invited,
		Joined
	}

	public class GameUser
	{
		public string Name { get; set; }
		public string ConnectionId { get; set; }
		public GameUserStatus Status { get; set; }
	}

	public class Invitation
	{
		private object m_lock = new object();

		public Invitation(string id, GameUser inviter, IEnumerable<GameUser> invitedUsers)
		{
			Id = id;
			Inviter = inviter;
			foreach (var user in invitedUsers)
			{
				InvitedUsers.Add(user);
			}
		}

		public string Id { get; private set; }
		public GameUser Inviter { get; private set; }
		public IList<GameUser> InvitedUsers { get; } = new List<GameUser>();
		public IList<GameUser> AllUsers
		{
			get
			{
				var result = new List<GameUser>();
				result.Add(Inviter);
				result.AddRange(InvitedUsers);

				return result;
			}
		}

		public bool AllInvitedUsersHaveJoined()
		{
			if (InvitedUsers.Count <= 0)
			{
				return false;
			}

			foreach (var user in InvitedUsers)
			{
				if (user.Status != GameUserStatus.Joined)
				{
					return false;
				}
			}

			return true;
		}

		public void Accept(string connectionId)
		{
			var user = InvitedUsers.FirstOrDefault(u => u.ConnectionId == connectionId);
			if (user != null)
			{
				user.Status = GameUserStatus.Joined;
			}
		}

		public void Decline(string connectionId)
		{
			var user = InvitedUsers.FirstOrDefault(u => u.ConnectionId == connectionId);
			if (user != null)
			{
				lock (m_lock)
				{
					if (InvitedUsers.Remove(user))
					{
						user.Status = GameUserStatus.NotInvitedYet;
					}
				}
			}
		}
	}

	public class Game
	{
		public Game(string id, IEnumerable<GameUser> users)
		{
			Id = id;
			foreach (var user in users)
			{
				Users.Add(user);
			}
		}

		public string Id { get; private set; }
		public IList<GameUser> Users { get; } = new List<GameUser>();
	}

	public interface IGameService
	{
		GameUser SignIn(string name, string connectionId);
		void SignOut(string connectionId);

		Invitation CreateInvitation(string inviterId, IEnumerable<string> connectionIdsToInvite);
		Invitation GetInvitation(string invitationId);
		bool RemoveInvitation(string invitationId);

		IList<Invitation> GetInvitationsForUser(string connectionId);
		IList<Invitation> GetMyInvitations(string connectionId);

		Game CreateGame(Invitation invitation);
		bool RemoveGame(string gameId);
		IList<Game> GetMyGames(string connectionId);

		GameUser GetUserById(string connectionId);
		GameUser GetUserByName(string name);

		IEnumerable<GameUser> GetUsersByIds(IEnumerable<string> connectionIds);
		IList<GameUser> Users { get; }
		IList<Game> Games { get; }
	}
}
