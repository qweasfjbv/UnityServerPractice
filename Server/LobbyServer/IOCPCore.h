#pragma once

namespace LobbyServer {

	class IOCPCore
	{
	public:
		IOCPCore(HANDLE iocpHandle);
		void HandleCompletion(BOOL ok, DWORD bytes, ULONG_PTR key, OVERLAPPED* overlapped);
	};

}