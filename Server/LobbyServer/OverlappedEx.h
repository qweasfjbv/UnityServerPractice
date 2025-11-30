/////////////////////////////////////////////
//
//	- Overlapped Extension
//
//  - Extension of Overlapped Struct
//	  - with Buffer, Operation Type
//
/////////////////////////////////////////////

#pragma once

#include <Winsock2.h>
#include <Windows.h>

namespace LobbyServer {

    enum class OperationType
    {
        RECV,
        SEND
    };

    struct OverlappedEx
    {
        WSAOVERLAPPED overlapped = {};
        WSABUF wsaBuf = {};
        char buffer[1024];
        OperationType opType;
    };
}