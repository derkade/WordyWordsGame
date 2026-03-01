"""
Filter commonwords.txt by checking each word against the Free Dictionary API.
Removes abbreviations, codes, and non-dictionary words.
Saves progress periodically so it can resume if interrupted.
"""

import asyncio
import aiohttp
import os
import json
import time

COMMON_PATH = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "commonwords.txt")
PROGRESS_PATH = os.path.join(os.path.dirname(__file__), "filter_common_progress.json")
OUTPUT_PATH = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "commonwords_filtered.txt")

API_URL = "https://api.dictionaryapi.dev/api/v2/entries/en/{}"
CONCURRENCY = 15
SAVE_INTERVAL = 200
RETRY_DELAY = 3.0
MAX_RETRIES = 5


def load_words(path):
    with open(path, "r", encoding="utf-8") as f:
        return [line.strip().upper() for line in f if line.strip()]


def load_progress():
    if os.path.exists(PROGRESS_PATH):
        with open(PROGRESS_PATH, "r") as f:
            return json.load(f)
    return {"checked": {}, "valid": [], "invalid": []}


def save_progress(progress):
    with open(PROGRESS_PATH, "w") as f:
        json.dump(progress, f)


async def check_word(session, word, semaphore, progress, stats):
    if word.lower() in progress["checked"]:
        return

    async with semaphore:
        for attempt in range(MAX_RETRIES):
            try:
                async with session.get(API_URL.format(word.lower()), timeout=aiohttp.ClientTimeout(total=10)) as resp:
                    stats["requests"] += 1
                    if resp.status == 200:
                        progress["checked"][word.lower()] = True
                        progress["valid"].append(word)
                        stats["valid"] += 1
                        return
                    elif resp.status == 404:
                        progress["checked"][word.lower()] = False
                        progress["invalid"].append(word)
                        stats["invalid"] += 1
                        return
                    elif resp.status == 429:
                        wait = RETRY_DELAY * (attempt + 2)
                        stats["rate_limits"] += 1
                        await asyncio.sleep(wait)
                        continue
                    else:
                        await asyncio.sleep(RETRY_DELAY)
                        continue
            except (aiohttp.ClientError, asyncio.TimeoutError):
                stats["errors"] += 1
                await asyncio.sleep(RETRY_DELAY)
                continue

        # Failed after retries - keep the word (benefit of the doubt)
        progress["checked"][word.lower()] = True
        progress["valid"].append(word)
        stats["failed_kept"] += 1


async def main():
    print("Loading common words...")
    all_words = load_words(COMMON_PATH)
    progress = load_progress()

    already_checked = len(progress["checked"])
    if already_checked > 0:
        print(f"Resuming: {already_checked} words already checked")

    words_to_check = [w for w in all_words if w.lower() not in progress["checked"]]

    total = len(words_to_check)
    print(f"Total common words: {len(all_words)}")
    print(f"Already checked: {already_checked}")
    print(f"Words to check: {total}")
    print()

    if total == 0:
        print("Nothing to check!")
    else:
        stats = {"requests": 0, "valid": 0, "invalid": 0, "errors": 0, "rate_limits": 0, "failed_kept": 0}
        semaphore = asyncio.Semaphore(CONCURRENCY)
        start_time = time.time()

        connector = aiohttp.TCPConnector(limit=CONCURRENCY, limit_per_host=CONCURRENCY)
        async with aiohttp.ClientSession(connector=connector) as session:
            batch = []
            for i, word in enumerate(words_to_check):
                batch.append(check_word(session, word, semaphore, progress, stats))

                if len(batch) >= SAVE_INTERVAL:
                    await asyncio.gather(*batch)
                    batch = []
                    save_progress(progress)

                    elapsed = time.time() - start_time
                    checked = stats["requests"]
                    rate = checked / elapsed if elapsed > 0 else 0
                    remaining = total - (i + 1)
                    eta_sec = remaining / rate if rate > 0 else 0
                    eta_min = eta_sec / 60

                    print(f"  [{i+1}/{total}] valid={stats['valid']} invalid={stats['invalid']} "
                          f"rate={rate:.1f}/s eta={eta_min:.1f}min "
                          f"(errs={stats['errors']} ratelimits={stats['rate_limits']})")

            if batch:
                await asyncio.gather(*batch)
                save_progress(progress)

        elapsed = time.time() - start_time
        print(f"\nDone! Checked {stats['requests']} words in {elapsed/60:.1f} minutes")
        print(f"  Valid: {stats['valid']}")
        print(f"  Invalid (removed): {stats['invalid']}")
        print(f"  Errors (kept): {stats['failed_kept']}")
        print(f"  Rate limits hit: {stats['rate_limits']}")

    # Write filtered list
    valid_set = set(w.lower() for w, v in progress["checked"].items() if v)
    output_words = [w for w in all_words if w.lower() in valid_set]
    output_words.sort()

    with open(OUTPUT_PATH, "w", encoding="utf-8") as f:
        for w in output_words:
            f.write(w + "\n")

    removed = len(all_words) - len(output_words)
    print(f"\nFiltered common words written to: {OUTPUT_PATH}")
    print(f"  Original: {len(all_words)} words")
    print(f"  Filtered: {len(output_words)} words")
    print(f"  Removed:  {removed} words")

    # Show some of the removed words
    invalid_words = sorted(set(all_words) - set(output_words))
    if invalid_words:
        print(f"\nSample removed words: {', '.join(invalid_words[:50])}")


if __name__ == "__main__":
    asyncio.run(main())
