
    // 获取 grid 容器
    var gridContainer = document.getElementById("grid-container");
    var currentpiece="X";
    var cells = [];
    resetBoard();
     // 创建 15x15 的 div 网格
    function resetBoard(){
        gridContainer.innerHTML="";
        for (var i = 0; i < 15; i++) {
            for (var j = 0; j < 15; j++) {
                // 创建 div 元素
                var div = document.createElement("div");
                div.className = "grid-item";
                div.addEventListener('click',setpieces);
                div.dataset.row=i;
                div.dataset.col=j;
                div.addEventListener('mouseover',showPlace);
                // 添加到 grid 容器中
                gridContainer.appendChild(div);
                var item = document.createElement("button");
                item.className = "piece";
                div.appendChild(item);
                cells.push("A");
            }
        }
        
        currentpiece = "X";
    }
   
    
    
    function setpieces(){
        var button = this.querySelector('.piece');
        const row=parseInt(this.dataset.row);
        const col=parseInt(this.dataset.col);
        cells[row*15+col]=currentpiece;
        console.log(cells[row*15+col]);
        if(checkWinner(parseInt(this.dataset.row),parseInt(this.dataset.col))){
            currentpiece=currentpiece=="X"?"黑色玩家":"白色玩家";
            alert(`${currentpiece} 胜利！`);
                    resetBoard();
        }else{
            console.log("123123");
            if(currentpiece=="X"){
                button.style.backgroundColor='black';
                currentpiece="O";
            }else{
                button.style.backgroundColor='white';
                currentpiece="X";
            }
        }
    }

    function showPlace(){
        var place = document.getElementById("place");
        const row=parseInt(this.dataset.row);
        const col=parseInt(this.dataset.col);
        place.textContent = row + " " + col ;
    }
    function checkWinner(row, col) {
            // 检查水平方向
            if (checkLine(row, col, 1, 0) + checkLine(row, col, -1, 0) >= 4) return true;
            // 检查垂直方向
            if (checkLine(row, col, 0, 1) + checkLine(row, col, 0, -1) >= 4) return true;
            // 检查左斜方向
            if (checkLine(row, col, 1, 1) + checkLine(row, col, -1, -1) >= 4) return true;
            // 检查右斜方向
            if (checkLine(row, col, 1, -1) + checkLine(row, col, -1, 1) >= 4) return true;
            return false;
    }
    // 检查指定方向上的连续棋子数
    function checkLine(row, col, rowDelta, colDelta) {
        var count = 0;
        var r = row + rowDelta;
        var c = col + colDelta;
        
        while (r >= 0 && r < 15 && c >= 0 && c < 15 && cells[r * 15 + c] == currentpiece) {
            count++;
            r += rowDelta;
            c += colDelta;
        }
        return count;
    }