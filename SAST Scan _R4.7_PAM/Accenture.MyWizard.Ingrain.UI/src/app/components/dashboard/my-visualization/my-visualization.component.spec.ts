import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { MyVisualizationComponent } from './my-visualization.component';

describe('MyVisualizationComponent', () => {
  let component: MyVisualizationComponent;
  let fixture: ComponentFixture<MyVisualizationComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ MyVisualizationComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MyVisualizationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
