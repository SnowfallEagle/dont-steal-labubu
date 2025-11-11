if [[ -z "$1" ]]; then
	echo "Error: Enter remote origin (https/ssh address of git repo) as first argument"
	exit 0
else
	echo "$1"
fi

git init
git add .
git rm --cached -r Assets
git commit -m "No assets"
git remote add origin "$1" 
git push -u origin master

git add .
cd Assets
git restore --staged _Template _Game
git commit -m "No _Template and _Game"
git push

git add _Template
git commit -m "No _Game"
git push

git add _Game
git commit -m "_Game"
git push

echo "Repo initialized successfuly"
