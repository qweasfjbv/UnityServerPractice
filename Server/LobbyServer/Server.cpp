#include "Server.h"
#include "IOCPCore.h"
#include "ClientSession.h"
#include "Logger.h"

namespace LobbyServer {

	Server::Server() :
		m_listenSocket(INVALID_SOCKET),
		m_iocpCore(nullptr) { }

	Server::~Server()
	{
		Stop();
	}

	bool Server::Init(uint16_t port)
	{
		if (!InitWinsock()) return false;
		if (!InitListenSocket(port)) return false;

		m_iocpCore = new IOCPCore();
		if (!m_iocpCore->InitIOCP()) return false;

		LOG_INFO("Server Init Success!");
		return true;
	}

	bool Server::InitWinsock()
	{
		WSADATA wsaData;
		int res = WSAStartup(MAKEWORD(2, 2), &wsaData);
		if (res != 0) LOG_ERROR("Winsock Startup Failed!");
		return res == 0;
	}

	bool Server::InitListenSocket(uint16_t port)
	{
		m_listenSocket = WSASocket(AF_INET, SOCK_STREAM, 0, NULL, 0, WSA_FLAG_OVERLAPPED);
		if (m_listenSocket == INVALID_SOCKET) return false;

		SOCKADDR_IN addr = {};
		addr.sin_family = AF_INET;
		addr.sin_addr.S_un.S_addr = htonl(INADDR_ANY);
		addr.sin_port = htons(port);

		if (bind(m_listenSocket, (SOCKADDR*)&addr, sizeof(addr)) == SOCKET_ERROR)
		{
			LOG_ERROR("Listen Socket Bind Failed!");
			return false;
		}

		if (listen(m_listenSocket, SOMAXCONN) == SOCKET_ERROR)
		{
			LOG_ERROR("Listen Socket Listen Failed!");
			return false;
		}

		LOG_INFO(std::format("Listening on PORT : {}", port));
		return true;
	}

	void Server::Run()
	{
		m_iocpCore->Run();
		LOG_INFO("Server Running ...");

		while (m_iocpCore->IsRunning())
		{
			// HACK - Upgrade to AcceptEx
			SOCKET clientSock = accept(m_listenSocket, NULL, NULL);
			if (clientSock == INVALID_SOCKET) continue;

			ClientSession* session = new ClientSession(clientSock);

			m_iocpCore->RegisterSession(session);
			session->PostRecv();
		}
	}

	void Server::Stop()
	{
		if (!m_iocpCore->IsRunning()) return;
		m_iocpCore->Stop();

		if (m_listenSocket != INVALID_SOCKET)
		{
			closesocket(m_listenSocket);
			m_listenSocket = INVALID_SOCKET;
		}

		delete m_iocpCore;
		WSACleanup();
	}

}