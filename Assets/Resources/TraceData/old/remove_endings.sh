for file in *.json
do 
   cat $file | sed '1s/^},/{"spans":[/' > tmp && echo "}]}" >> tmp && mv tmp $file
done
