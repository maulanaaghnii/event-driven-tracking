# WebSocket server listener di 8767
import asyncio
import websockets

async def handler(websocket):
    async for message in websocket:
        print(f"Received on 8767: {message}")

async def main():
    print("Listening on ws://localhost:8767")
    async with websockets.serve(handler, "localhost", 8767):
        await asyncio.Future()  # Run forever

asyncio.run(main())
