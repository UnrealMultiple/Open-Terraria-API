﻿namespace OTA
{
    /// <summary>
    /// Known Terraria packets
    /// </summary>
    // TODO Pascal case these, and correct the name if incorrect...anyone?...hello?
    public enum Packet : int
    {
        CONNECTION_REQUEST = 1,
        DISCONNECT = 2,
        CONNECTION_RESPONSE = 3,
        PLAYER_DATA = 4,
        INVENTORY_DATA = 5,
        WORLD_REQUEST = 6,
        WORLD_DATA = 7,
        REQUEST_TILE_BLOCK = 8,
        SEND_TILE_LOADING = 9,
        SEND_TILE_ROW = 10,
        SEND_TILE_CONFIRM = 11,
        RECEIVING_PLAYER_JOINED = 12,
        PLAYER_STATE_UPDATE = 13,
        SYNCH_BEGIN = 14,
        UPDATE_PLAYERS = 15,
        PLAYER_HEALTH_UPDATE = 16,
        TILE_BREAK = 17,
        TIME_SUN_MOON_UPDATE = 18,
        BARRIER_STATE_CHANGE = 19,
        TILE_SQUARE = 20,
        ITEM_INFO = 21,
        ITEM_OWNER_INFO = 22,
        NPC_INFO = 23,
        STRIKE_NPC = 24,
        PLAYER_CHAT = 25,
        STRIKE_PLAYER = 26,
        PROJECTILE = 27,
        DAMAGE_NPC = 28,
        KILL_PROJECTILE = 29,
        PLAYER_PVP_CHANGE = 30,
        OPEN_CHEST = 31,
        CHEST_ITEM = 32,
        PLAYER_CHEST_UPDATE = 33,
        CHEST = 34,
        HEAL_OTHER_PLAYER = 35,
        ENTER_ZONE = 36,
        PASSWORD_REQUEST = 37,
        PASSWORD_RESPONSE = 38,
        ITEM_OWNER_UPDATE = 39,
        NPC_TALK = 40,
        PLAYER_BALLSWING = 41,
        PLAYER_MANA_UPDATE = 42,
        PLAYER_USE_MANA_UPDATE = 43,
        KILL_PLAYER_PVP = 44,
        PLAYER_JOIN_PARTY = 45,
        READ_SIGN = 46,
        WRITE_SIGN = 47,
        FLOW_LIQUID = 48,
        SEND_SPAWN = 49,
        PLAYER_BUFFS = 50,
        SUMMON_SKELETRON = 51,

        //1.0.6
        CHEST_UNLOCK = 52,
        NPC_ADD_BUFF = 53,
        NPC_BUFFS = 54,
        PLAYER_ADD_BUFF = 55,

        //1.1
        NPC_NAME = 56,
        WORLD_BALANCE = 57,
        PLAY_HARP = 58,
        HIT_SWITCH = 59,
        NPC_HOME = 60,

        //1.1.2
        SPAWN_NPCS = 61,

        //Since TDSM left
        PLAYER_DODGE = 62,
        PAINT_TILE = 63,
        PAINT_WALL = 64,
        TELEPORT = 65,
        HEAL_PLAYER = 66,
        PACKET_67 = 67,
        CLIENT_UUID = 68,
        CHEST_NAME_UPDATE = 69,
        CATCH_NPC = 70,
        RELEASE_NPC = 71,
        TRAVEL_SHOP = 72,
        TELEPORTATION_POTION = 73,
        ANGLER_QUEST = 74,
        ANGLER_FINISH_REGISTER = 75,
        ANGLER_QUESTS_FINISHED = 76,

        //Up to 105 is missing

        //custom
        TILE_ROW_COMPRESSED = 240,
        //		TILE_SQUARE_COMPRESSED = 241,

    }
}
