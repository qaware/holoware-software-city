for file in *.json
do
   awk '/@timestamp/ || /"us"/ || /"name": ".*#.*"/' $file > tmp && mv tmp $file
done
