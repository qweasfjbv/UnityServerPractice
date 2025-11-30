#include "Server.h"
#include "Logger.h"

using namespace LobbyServer;

#define LOBBY_PORT 61394

int main()
{
	Server server;

	if (!server.Init(LOBBY_PORT))
	{
		LOG_ERROR("Server Init Failed!");
		return -1;
	}

	server.Run();

	return 0;
}