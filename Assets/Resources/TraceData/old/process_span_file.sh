#!/bin/bash
for file in *.json
do
   cat $file | sed "s/\"@timestamp\"/},{\"@timestamp\"/" | sed -E "s/\"us\": ([0-9]{13})/\"timestamp_us\": \1/" | sed -E "s/\"us\": ([0-9]+)/\"duration_us\": \1/" | sed -E "s/[ ]{7,}/,/" | sed 's/,$//' | sed 's/,}/}/' | sed '1s/^},/{"spans":[/' > tmp && echo "}]}" > tmp && mv tmp $file
done
