import os
import sys
import asyncio
from pathlib import Path
from argparse import ArgumentParser


try:
    CS_APP_PATH: Path = Path(os.getenv("GET_TVSHOW_TOTAL_LENGTH_BIN"))
except TypeError as e:
    print("Environment variable GET_TVSHOW_TOTAL_LENGTH_BIN doesn't exist. Exiting.")
    exit(1)


async def get_show_length(show: str, lengths_by_show: dict[str, int]) -> int:
    process = await asyncio.create_subprocess_exec(
        CS_APP_PATH,
        f'"{show}"',
        stdout=asyncio.subprocess.PIPE,
        stderr=asyncio.subprocess.PIPE
    )
    stdout, _ = await process.communicate()
    if process.returncode == 0:
        lengths_by_show[show] = int(stdout.decode().strip())
    else:
        print(f"Could not get info for '{show}'.", file=sys.stderr)


def format_item(item: tuple[str, int]) -> str:
    show = item[0]
    hours = int(item[1] / 60)
    minutes = int(item[1] % 60)
    return f"{show} ({hours}h {minutes}m)"


def find_shortest_and_longest(lengths_by_show: dict[str, int]) -> None:
    sorted_dict = dict(sorted(lengths_by_show.items(), key=lambda item: item[1]))
    shortest = next(iter(sorted_dict.items()))
    longest = next(iter(reversed(sorted_dict.items())))
    print(f"The shortest show: {format_item(shortest)}")
    print(f"The longest show: {format_item(longest)}")


async def main() -> None:
    arg_parser = ArgumentParser()
    arg_parser.add_argument("SHOW_LIST_FILE")
    args = arg_parser.parse_args()
    show_list_file = args.SHOW_LIST_FILE
    show_list_file_path = Path(show_list_file)

    if not os.path.exists(CS_APP_PATH):
        print(f"Path {CS_APP_PATH} doesn't exist. Make sure to build C# project with 'dotnet build GetTvShowTotalLength.csproj'")
        print(f"Exiting.")
        exit(0)

    with open(show_list_file_path, "r") as file:
        show_list = file.read().splitlines()

    tasks = []
    lengths_by_show: dict[str, int] = {}
    for show in show_list:
        tasks.append(get_show_length(show, lengths_by_show))
    await asyncio.gather(*tasks)

    find_shortest_and_longest(lengths_by_show)

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except RuntimeError as e:
        print(f"Runtime Error: {e}")
        print("Make sure to build C# project with 'dotnet build GetTvShowTotalLength.csproj'")
        print("Exiting.")
        exit(2)
