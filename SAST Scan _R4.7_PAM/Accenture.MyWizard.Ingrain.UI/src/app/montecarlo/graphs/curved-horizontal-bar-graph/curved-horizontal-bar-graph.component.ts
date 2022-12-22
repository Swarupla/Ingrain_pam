import { Component, ElementRef, OnInit, ViewChild, OnChanges, Input } from '@angular/core';
import * as d3 from 'd3';
import { NotificationService } from 'src/app/_services/notification-service.service';

@Component({
  selector: 'app-curved-horizontal-bar-graph',
  templateUrl: './curved-horizontal-bar-graph.component.html',
  styleUrls: ['./curved-horizontal-bar-graph.component.scss']
})
export class CurvedHorizontalBarGraphComponent implements OnInit, OnChanges {

  @ViewChild('horizontalBarContainer', { static: true }) containerBarChart: ElementRef;
  @Input() horizontalChart;
  @Input() data;
  margin = { top: 20, right: 20, bottom: 40, left: 60 }; //  top: 20
  width = 257;
  height = 212;
  x; y; max; min; svg; element; xAxis; yAxis; yMax; yMin;
  values;
  @Input() teamSizeCapturedFlag;
  @Input() generic;

  constructor(private ns: NotificationService) { }

  ngOnInit() {
    if ( this.generic === 'generic') {
     this.width = 500;
    }
    if (this.data) {
      const transformedData = this.transformData(this.data);
      this.createHorizontalGraph(transformedData);
    }
  }

  ngOnChanges() {
    this.element = this.containerBarChart.nativeElement; console.log('curved chart--', this.horizontalChart, this.teamSizeCapturedFlag);
    if (this.data) {
      const transformedData = this.transformData(this.data);
      // if (this.horizontalChart === 'Team Size' && this.teamSizeCapturedFlag === 0) {
      //   this.errorMessageGraph();
      // } else {
        this.createHorizontalGraph(transformedData);
      // }
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

  createHorizontalGraph(dataset) {
    const _this = this;
    const data = dataset;

    const height1 = this.height - this.margin.top - this.margin.bottom;
    const width1 = this.width - this.margin.left - this.margin.right;

    d3.select(this.element).html('');
    const svg = d3.select(this.element)
      .append('svg')
      .attr('width', width1 + this.margin.left + this.margin.right)
      .attr('height', height1 + this.margin.top + this.margin.bottom);

    const graphArea = svg
      .append('g')
      .attr('transform', 'translate(' + this.margin.left + ',' + this.margin.top + ')');

    const yLabelText = d3.select(this.element).append('div')
      .attr('id', 'yFullText')
      .style('display', 'none')
      .attr('class', 'tooltip')
      .style('font-size', '0.7rem')
       .style('background-color', 'rgba(0, 0, 0, 0.8)')
       .style('color', 'white')
      // .style('border-radius', '2px')
      // .style('padding', '2px');

    // Add axis labels
    let traX =  (width1 - this.margin.right);
    if ( this.generic === 'generic') {
      traX =  (width1 - this.margin.right - 100);
    }
    svg.append('text')
      .attr('class', 'x label')
      .attr('transform', 'translate(' + traX + ' ,' + (height1 + this.margin.bottom) + ')')
      // .attr('dy', '1em')
      .attr('text-anchor', 'middle')
      .attr('dy', '0.9em')
      .style('font-size', '0.7rem')
      .style('font-weight', '700')
      .style('fill', '#7a7a7a')
      .text('Percentage');
    
      let yLabel = 'Phase'; 
    if ( this.generic === 'generic') {
      yLabel = 'Influencer'
    }  

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
      .text(yLabel);

    const x = d3.scaleLinear()
      .range([0, width1])
      .domain([
        d3.min(data, d => d.value),
        d3.max(data, d => d.value)
      ])
      .nice();

    const y = d3.scaleBand()
      .rangeRound([0, height1])
      .domain(data.map(d => d.name))
      .padding(0.4);

    const xAxisGrid = d3.axisBottom(x).ticks(5).tickSize(-height1).tickFormat('');
    // const yAxisGrid = d3.axisLeft(y).tickSize(-width1).tickFormat('');


    const xAxis = d3.axisBottom(x).ticks(5).tickFormat(d => d + '%');
    const yAxis = d3.axisLeft(y);

    // Create grids.
    graphArea.append('g')
      .attr('class', 'x xaxis-grid')
      .attr('transform', 'translate(0,' + height1 + ')')
      .call(xAxisGrid)
      .selectAll('.tick')
      .attr('stroke', 'lightblue')
      .attr('opacity', '0.05');
    /* graphArea.append('g')
      .attr('class', 'y yaxis-grid')
      .call(yAxisGrid)
      .selectAll('.tick')
      .attr('stroke', 'lightblue')
      .attr('opacity', '0.05'); */

    // Create axis
    graphArea
      .append('g')
      .attr('class', 'x-Axis')
      .attr('transform', `translate(0, ${height1})`)
      .call(xAxis)
      .selectAll('.tick text')
      .style('fill', '#7a7a7a');

    graphArea
      .append('g')
      .attr('class', 'y-Axis')
      .call(yAxis)
      .selectAll('.tick text')
      .style('fill', '#7a7a7a')
      .call(wrap, y.bandwidth())
      .on('mouseover', (d, index, elements) => {
        const coords = [30, (index * 19) + 60];
        this.showFullText(d, coords);
      })
      .on('mouseleave', this.hideTooltip);

    d3.select('g.x-Axis').select('path').attr('stroke', 'lightgrey').attr('opacity', '0.1');
    d3.select('g.y-Axis').select('path').attr('stroke', 'lightgrey').attr('opacity', '0.1');
    d3.select('g.x-Axis').selectAll('.tick line').remove();
    d3.select('g.y-Axis').selectAll('.tick line').remove();
    d3.select('g.xaxis-grid').select('path').remove();
    d3.select('g.yaxis-grid').select('path').remove();

    const wrapText = _this.wrapWordEllipse;
    svg.selectAll('g.y-Axis text')
      .call(wrapText, 30);

    const rx = y.bandwidth() / 2;
    const ry = y.bandwidth() / 2;

    graphArea
      .selectAll('bar')
      .data(data)
      .enter().append('path')
      .style('fill', '#47a0ff')
      .attr('d', function (item, i) {
        if ((x(item.value) - x(0) - rx) < 0 || (x(item.value) - rx) < 0) {
          return `
              M0,${y(item.name)}
              h0
              a${rx},${ry} 0 0 1 ${rx},${ry}
              v0
              a${rx},${ry} 0 0 1 ${-rx},${ry}
              h0Z
            `;
        } else {
          return `
              M0,${y(item.name)}
              h${x(item.value) - rx}
              a${rx},${ry} 0 0 1 ${rx},${ry}
              v0
              a${rx},${ry} 0 0 1 ${-rx},${ry}
              h${-(x(item.value) - rx)}Z
            `;
        }
      });
    /*  .attr('d', item => `
       M${x(0)},${y(item.name)}
       h${x(item.value) - x(0) - rx}
       a${rx},${ry} 0 0 1 ${rx},${ry}
       v${y.bandwidth() - 2 * rx}
       a${rx},${ry} 0 0 1 ${-rx},${ry}
       h${-(x(item.value) - x(0) - rx)}Z
     `); */

    function wrap(text, width) {
      text.each(function () {
        const text = d3.select(this),
          words = text.text().split(/\s+/).reverse();
        let word,
          line = [],
          lineNumber = 0;
        const lineHeight = 1.1, // ems
          x = text.attr('x'),
          y = text.attr('y'),
          dy = 0,
          dx = 4;
        let tspan = text.text(null)
          .append('tspan')
          .attr('x', x)
          .attr('dx', dx)
          .attr('y', y)
          .attr('dy', dy + 'em');
        while (word = words.pop()) {
          line.push(word);
          tspan.text(line.join(' '));
          if ((tspan.node().getComputedTextLength() - 40) > width) {
            line.pop();
            tspan.text(line.join(' '));
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

  wrapWordEllipse(label, width) {
    label.each(function () {
      let text = d3.select(this);
      text = text._groups[0][0];
      if (text.textContent.length > 10) {
        text.textContent = text.textContent.substring(0, 5) + '..';
      }
    });
  }

  showFullText(d, coords) {
    const tooltipDiv = d3.select('#yFullText');
    tooltipDiv.transition().duration(100).style('opacity', 1);
    tooltipDiv.html(d);
    tooltipDiv.style('display', 'block');
   
    tooltipDiv.style('left',  d3.event.layerX + 'px');
    tooltipDiv.style('top',   (d3.event.layerY + 20 )  + 'px');
  }

  hideTooltip(d) {
    const tooltipDiv = d3.select('#yFullText');
    tooltipDiv.style('display', 'none');
  }

  errorMessageGraph() {
    d3.select(this.element).html('');

    const height1 = this.height - this.margin.top - this.margin.bottom;
    const width1 = this.width - this.margin.left - this.margin.right;

    const svg = d3.select(this.element)
      .append('svg')
      .attr('width', width1 + this.margin.left + this.margin.right)
      .attr('height', height1 + this.margin.top + this.margin.bottom);

    const graphArea = svg
      .append('g')
      .attr('transform', 'translate(' + this.margin.left + ',' + this.margin.top + ')');

    // Add axis labels
    svg.append('text')
      .attr('class', 'x label')
      .attr('transform', 'translate(' + ((width1 / 2) + 32) + ' ,' + (height1 / 2) + ')')
      // .attr('dy', '1em')
      .attr('text-anchor', 'middle')
      .attr('dy', '0.9em')
      .style('font-size', '0.7rem')
      .style('font-weight', '700')
      .style('fill', '#7a7a7a')
      .text('No Phases captured at team size level');
  }

}
