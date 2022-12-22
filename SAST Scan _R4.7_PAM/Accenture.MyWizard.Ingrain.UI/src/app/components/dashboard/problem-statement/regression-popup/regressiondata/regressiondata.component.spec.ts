import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RegressiondataComponent } from './regressiondata.component';

describe('RegressiondataComponent', () => {
  let component: RegressiondataComponent;
  let fixture: ComponentFixture<RegressiondataComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RegressiondataComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RegressiondataComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
