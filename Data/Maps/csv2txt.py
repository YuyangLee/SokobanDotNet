'''
Author: Aiden Li
Date: 2023-04-02 22:30:08
LastEditors: Aiden Li (i@aidenli.net)
LastEditTime: 2023-04-02 22:38:06
Description: Turn CSV file to a TXT file that describes the game.
'''
import csv
import os
import argparse

parser = argparse.ArgumentParser()

parser.add_argument("--in_file", type=str, default="Basic.csv", help="Input CSV file.")

args = parser.parse_args()

in_file = args.in_file
out_file = in_file[:-4] + ".txt"

data = list(csv.reader(open(in_file, 'r', encoding='utf-8-sig')))

with open(out_file, 'w', encoding='utf-8') as f:
    for row in data:
        f.write("".join(row) + "\n")
    f.close()
