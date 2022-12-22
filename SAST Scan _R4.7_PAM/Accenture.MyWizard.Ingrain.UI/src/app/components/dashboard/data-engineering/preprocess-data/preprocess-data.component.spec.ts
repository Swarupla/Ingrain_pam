import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PreprocessDataComponent } from './preprocess-data.component';

describe('PreprocessDataComponent', () => {
  let component: PreprocessDataComponent;
  let fixture: ComponentFixture<PreprocessDataComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PreprocessDataComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PreprocessDataComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
