import { Component, ElementRef, EventEmitter, Input, OnInit, Output, ViewChild, OnChanges } from '@angular/core';
import * as d3 from 'd3';

@Component({
  selector: 'app-curved-vertical-bar-graph',
  templateUrl: './curved-vertical-bar-graph.component.html',
  styleUrls: ['./curved-vertical-bar-graph.component.scss']
})
export class CurvedVerticalBarGraphComponent implements OnInit, OnChanges {

  @ViewChild('verticalBarContainer', { static: true }) containerBarChart: ElementRef;
  @Input() data;
  @Input() inputWidth;
  @Input() inputHeight;
  @Input() xAxisName;
  @Input() yAxisName;

  margin = { top: 20, right: 20, bottom: 40, left: 40 }; //  top: 20
  width = 257; // 416; // 360;
  height = 212; // 230;
  host;
  svg;

  @Output() barClicked = new EventEmitter();

  constructor() { }

  ngOnInit() {
  }

  ngOnChanges() {
    if (this.data) {
      const transformedData = this.transformData(this.data);
      this.createVerticalBarGraph(transformedData);
    }
  }

  transformData(data) {
    const dataArray = [];
    Object.entries(data).forEach(
      ([key, value]) => {
        dataArray.push({ name: key, value: value });
      }
    );
    return dataArray;
  }

  createVerticalBarGraph(dataset) {
    const _this = this;

    const element = this.containerBarChart.nativeElement;
    element.innerText = '';

    const data = dataset;
    if (this.inputWidth) {
      this.width = this.inputWidth;
    }
    if (this.inputHeight) {
      this.height = this.inputHeight;
    }
    const height1 = this.height - this.margin.top - this.margin.bottom;
    const width1 = this.width - this.margin.left - this.margin.right;

    const svg = d3.select(element)
      .append('svg')
      .attr('width', width1 + this.margin.left + this.margin.right)
      .attr('height', height1 + this.margin.top + this.margin.bottom);

    if (this.inputHeight && this.inputWidth) {
      this.margin.left = 10;
    }
    const graphArea = svg
      .append('g')
      .attr('transform', 'translate(' + (this.margin.left + this.margin.right) + ',' + this.margin.top + ')');

    // Add axis labels
    const xlabel = (this.xAxisName) ? this.xAxisName : 'Influencer';
    const width = (this.xAxisName) ? (width1 / 2) : width1;
    svg.append('text')
      .attr('class', 'x label')
      .attr('transform', 'translate(' + (width - this.margin.left) + ' ,' + (height1 + this.margin.bottom) + ')')
      .attr('text-anchor', 'middle')
      .attr('dy', '0.9em')
      .style('font-size', '0.7rem')
      .style('font-weight', '700')
      .style('fill', '#7a7a7a')
      .text(xlabel);

    const ylabel = (this.yAxisName) ? this.yAxisName : 'Percentage';
    svg.append('text')
      .attr('class', 'y label')
      .attr('transform', 'rotate(-90)')
      .attr('y', 0)
      .attr('x', 0 - ((height1 + this.margin.top + this.margin.bottom) / 2))
      .attr('dy', '0.8em')
      .attr('text-anchor', 'middle')
      .style('font-size', '0.7rem')
      .style('font-weight', '700')
      .style('fill', '#7a7a7a')
      .text(ylabel);

    if (!this.inputHeight && !this.inputWidth) {
      const format = d3.format('.0%');
    }

    const x = d3.scaleBand()
      .rangeRound([0, width1])
      .domain(data.map(d => d.name))
      .padding(0.4);

    const y = d3.scaleLinear()
      .range([height1, 0])
      .domain([
        d3.min(data, d => d.value),
        d3.max(data, d => d.value)
      ])
      .nice();

    // const xAxisGrid = d3.axisBottom(x).ticks(5).tickSize(-height1).tickFormat('');
    const yAxisGrid = d3.axisLeft(y).ticks(5).tickSize(-width1).tickFormat('');

    const xAxis = d3.axisBottom(x);
    let yAxis;
    if (this.inputWidth && this.inputHeight) {
      yAxis = d3.axisLeft(y).ticks(5).tickFormat(d => d);
    } else {
      yAxis = d3.axisLeft(y).ticks(5).tickFormat(d => d + '%');
    }

    // Create grids.
    /* graphArea.append('g')
      .attr('class', 'x xaxis-grid')
      .attr('transform', 'translate(0,' + height1 + ')')
      .call(xAxisGrid)
      .selectAll('.tick')
      .attr('stroke', 'lightblue')
      .attr('opacity', '0.05');*/
    let yOpacity = '0.05';
    let yColor = 'lightblue';
    if (this.inputWidth) {
      yOpacity = '0.2';
      yColor = 'lightblue';
    }
    graphArea.append('g')
      .attr('class', 'y yaxis-grid')
      .call(yAxisGrid)
      .selectAll('.tick')
      .attr('stroke', yColor)
      .attr('opacity', yOpacity);

    // Create axis
    graphArea
      .append('g')
      .attr('class', 'xAxis')
      .attr('transform', `translate(0, ${height1})`)
      .call(xAxis)
      .selectAll('.tick text')
      .style('fill', '#7a7a7a')
      .attr('dx', '-5')
      .call(wrap, x.bandwidth());

    graphArea
      .append('g')
      .attr('class', 'yAxis')
      .call(yAxis)
      .selectAll('.tick text')
      .style('fill', '#7a7a7a')
      .style('visibility' , (_this.inputWidth) ? 'hidden' : 'visible');

    d3.select('g.xAxis').select('path').attr('stroke', 'lightgrey').attr('opacity', '0.1');
    d3.select('g.yAxis').select('path').attr('stroke', 'lightgrey').attr('opacity', '0.1');
    d3.select('g.xAxis').selectAll('.tick line').remove();
    d3.select('g.yAxis').selectAll('.tick line').remove();
    d3.select('g.xaxis-grid').select('path').remove();
    d3.select('g.yaxis-grid').select('path').remove();


    const rx = 12;
    const ry = 12;

    const bar = graphArea
      .selectAll('bar')
      .data(data)
      .enter();

    bar.append('path')
      .attr('class', 'rect')
      .style('fill', function (d, i) {
        if (i === 0) {
          return '#47a0ff';
        } else {
          return '#a3d0ff';
        }
      })
      /* .attr('d', item => `
        M${x(item.name)},${y(item.value) + ry}
        a${rx},${ry} 0 0 1 ${rx},${-ry}
        h${x.bandwidth() - 2 * rx}
        a${rx},${ry} 0 0 1 ${rx},${ry}
        v${height1 - y(item.value) - ry}
        h${-(x.bandwidth())}Z
      `) */
      .attr('d', function (item, i) {
        if ((height1 - y(item.value) - rx) < 0) {
          return `
        M${x(item.name)},${height1}
        v0
        a12,12 0 0 1 12,-12
        h${x.bandwidth() - 3 * rx}
        a12,12 0 0 1 12,12
        v0Z
      `;
        } else {
          return `
        M${x(item.name)},${height1}
        v${-(height1 - y(item.value) - rx)}
        a${rx},${ry} 0 0 1 ${rx},${-ry}
        h${x.bandwidth() - 3 * rx}
        a${rx},${ry} 0 0 1 ${rx},${ry}
        v${(height1 - y(item.value) - rx)}Z
      `;
        }
      })
      .on('click', function (item, i) {
        barClick(item.name, i);  // Interactive Charts pass which Bar is clicked
        d3.select(this).transition()
          .style('fill', '#47a0ff');
      });


    if ( this.inputHeight && this.inputWidth) {
    bar.append('text')
      .attr('class', 'label')
      // y position of the label is halfway down the bar
      .attr('y', function (item) {
        return ( y(item.value) === 0 ? 0 : (y(item.value) - 14) );
      })
      // x position is 3 pixels to the right of the bar
      .attr('x', function (item) {
        return x(item.name) + x.bandwidth() / 2;
      })
      .attr('font-size', 12)
      .attr('font-family', 'sans-serif')
      .text(function (d) {
        return d.value;
      });
    }

    // graphArea.selectAll('path').append('text')
    // .attr('class', 'label')
    // // y position of the label is halfway down the bar
    // .attr('y', function (item) {
    //   return y(item.value);
    // })
    // // x position is 3 pixels to the right of the bar
    // .attr('x', function (item) {
    //   return x(item.value);
    // })
    // .attr('font-size', 12)
    // .attr('font-family', 'sans-serif')
    // .text(function (d) {
    //   return d.name ;
    // });

    function barClick(name, index) {

      d3.selectAll('.rect').transition()
          .style('fill', function (d, i) {
            if (i !== index) {
              return '#a3d0ff';
            }
          });
      _this.barClicked.emit(name);
    }

    function wrap(text, width) {
      text.each(function () {
        let text = d3.select(this),
          words = text.text().split(/\s+/).reverse();
          if ( _this.inputHeight) {
            words = text.text().split('-').reverse();
          }
        let word,
          line = [],
          lineNumber = 0;
        const lineHeight = 1.1, // ems
          x = 0,
          y = text.attr('y'),
          dx = text.attr('dx'),
          dy = 0;
        let tspan = text.text(null)
          .append('tspan')
          .attr('x', x)
          .attr('dx', dx)
          .attr('y', y)
          .attr('dy', dy + 'em');
        while (word = words.pop()) {
          line.push(word);
          if ( _this.inputHeight) {
            tspan.text(line.join('-'));
          } else {
          tspan.text(line.join(' '));
          }
          if ((tspan.node().getComputedTextLength() - 10) > width) {
            line.pop();
            if ( _this.inputHeight) {
              tspan.text(line.join('-'));
            } else {
            tspan.text(line.join(' '));
            }
            line = [word];
            tspan = text.append('tspan')
              .attr('x', x)
              .attr('dx', dx)
              .attr('y', y)
              .attr('dy', ++lineNumber * lineHeight + dy + 'em')
              .text(word);
          }
        }
      });
    }

  }

}
